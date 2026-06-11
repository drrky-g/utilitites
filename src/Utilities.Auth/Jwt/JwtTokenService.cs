using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Utilities.Auth;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtTokenOptions _options;
    private readonly IRefreshTokenStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _handler;

    public JwtTokenService(JwtTokenOptions options, IRefreshTokenStore store, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(store);

        var vr = options.Validate(null, options);
        if (vr.Failed)
            throw new ArgumentException(string.Join("; ", vr.Failures!), nameof(options));

        _options = options;
        _store = store;
        _timeProvider = timeProvider ?? TimeProvider.System;

        var keyBytes = Convert.FromBase64String(options.SigningKeyBase64);
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);
        _handler = new JsonWebTokenHandler();
    }

    public async Task<OAuthToken> IssueTokenAsync(IAuthUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _timeProvider.GetUtcNow();
        var expires = now + _options.AccessTokenLifetime;

        var scopes = user.ClaimsPrincipal
            .FindAll(AuthClaimTypes.Scope)
            .Select(c => c.Value)
            .ToList();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(AuthClaimTypes.Provider, user.ProviderName),
        };
        foreach (var scope in scopes)
            claims.Add(new Claim(AuthClaimTypes.Scope, scope));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _signingCredentials,
        };

        var accessToken = _handler.CreateToken(descriptor);

        var refreshTokenBytes = RandomNumberGenerator.GetBytes(32);
        var refreshToken = Base64UrlEncoder.Encode(refreshTokenBytes);

        var entry = new RefreshTokenEntry
        {
            Token = refreshToken,
            UserId = user.UserId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            ProviderName = user.ProviderName,
            Scopes = scopes.AsReadOnly(),
            ExpiresAt = now + _options.RefreshTokenLifetime,
        };
        await _store.StoreAsync(entry, cancellationToken);

        return new OAuthToken
        {
            AccessToken = accessToken,
            TokenType = TokenTypes.Bearer,
            RefreshToken = refreshToken,
            ExpiresAt = expires,
            Scopes = scopes.AsReadOnly(),
            IssuedBy = _options.Issuer,
        };
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return TokenValidationResult.Failure(TokenValidationFailureReason.Malformed, "Token is null or empty.");

        var tvp = BuildValidationParameters();

        try
        {
            var msResult = await _handler.ValidateTokenAsync(token, tvp);
            if (!msResult.IsValid)
                return MapException(msResult.Exception);

            var jwt = (JsonWebToken)msResult.SecurityToken!;
            DateTimeOffset? expiresAt = jwt.ValidTo != DateTime.MinValue
                ? new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero)
                : null;

            if (expiresAt.HasValue)
            {
                var now = _timeProvider.GetUtcNow();
                if (now > expiresAt.Value + _options.ClockSkew)
                    return TokenValidationResult.Failure(TokenValidationFailureReason.Expired, $"Token expired at {expiresAt.Value}.");
            }

            return TokenValidationResult.Success(new ClaimsPrincipal(msResult.ClaimsIdentity), expiresAt);
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    public async Task<OAuthToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);

        var entry = await _store.FindAsync(refreshToken, cancellationToken);
        if (entry is null)
            throw new SecurityTokenException("Refresh token not found or has been revoked.");

        var now = _timeProvider.GetUtcNow();
        if (now >= entry.ExpiresAt)
        {
            await _store.RevokeAsync(refreshToken, cancellationToken);
            throw new SecurityTokenExpiredException($"Refresh token expired at {entry.ExpiresAt}.");
        }

        await _store.RevokeAsync(refreshToken, cancellationToken);

        var user = new StoredEntryAuthUser(entry);
        return await IssueTokenAsync(user, cancellationToken);
    }

    public ValueTask DisposeAsync() => _store.DisposeAsync();

    private TokenValidationParameters BuildValidationParameters() => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _signingCredentials.Key,
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = _options.Audience is not null,
        ValidAudience = _options.Audience,
        ValidateLifetime = false,
    };

    private static TokenValidationResult MapException(Exception? ex) =>
        ex switch
        {
            SecurityTokenExpiredException e => TokenValidationResult.Failure(TokenValidationFailureReason.Expired, e.Message),
            SecurityTokenSignatureKeyNotFoundException e => TokenValidationResult.Failure(TokenValidationFailureReason.InvalidSignature, e.Message),
            SecurityTokenInvalidSignatureException e => TokenValidationResult.Failure(TokenValidationFailureReason.InvalidSignature, e.Message),
            SecurityTokenInvalidIssuerException e => TokenValidationResult.Failure(TokenValidationFailureReason.InvalidIssuer, e.Message),
            SecurityTokenInvalidAudienceException e => TokenValidationResult.Failure(TokenValidationFailureReason.InvalidAudience, e.Message),
            SecurityTokenException e => TokenValidationResult.Failure(TokenValidationFailureReason.Unknown, e.Message),
            _ => TokenValidationResult.Failure(TokenValidationFailureReason.Malformed, ex?.Message),
        };

    private sealed class StoredEntryAuthUser : IAuthUser
    {
        private readonly ClaimsPrincipal _principal;

        public StoredEntryAuthUser(RefreshTokenEntry entry)
        {
            UserId = entry.UserId;
            DisplayName = entry.DisplayName;
            Email = entry.Email;
            ProviderName = entry.ProviderName;

            var identity = new AuthUserClaims(entry.UserId, entry.DisplayName, entry.Email, entry.ProviderName);
            foreach (var scope in entry.Scopes)
                identity.AddClaim(new Claim(AuthClaimTypes.Scope, scope));
            _principal = identity.ToClaimsPrincipal();
        }

        public string UserId { get; }
        public string DisplayName { get; }
        public string Email { get; }
        public string ProviderName { get; }
        public ClaimsPrincipal ClaimsPrincipal => _principal;

        public System.Security.Principal.IIdentity? Identity => _principal.Identity;
        public bool IsInRole(string role) => _principal.IsInRole(role);
    }
}
