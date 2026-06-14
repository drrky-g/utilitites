using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Utilities.Auth;

/// <summary>
/// <see cref="IOAuthProvider"/> implementation for Google. Builds authorization URLs (with PKCE), exchanges
/// authorization codes for tokens, and maps Google's user-info response onto an <see cref="IAuthUser"/>.
/// </summary>
public sealed class GoogleOAuthProvider : IOAuthProvider
{
    /// <summary>The <see cref="IHttpClientFactory"/> client name used by this provider.</summary>
    public const string HttpClientName = "Utilities.Auth.Google";

    private const string CodeChallengeMethod = "S256";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleOAuthConfig _config;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates a Google provider from an HTTP client factory and validated <see cref="GoogleOAuthConfig"/>.</summary>
    public GoogleOAuthProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleOAuthConfig> options,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);

        _httpClientFactory = httpClientFactory;
        _config = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public Task<string> BuildAuthorizationUrlAsync(string state, string codeChallenge, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        cancellationToken.ThrowIfCancellationRequested();

        var query = new List<KeyValuePair<string, string>>
        {
            new("client_id", _config.ClientId),
            new("redirect_uri", _config.RedirectUri),
            new("response_type", "code"),
            new("scope", string.Join(' ', _config.Scope)),
            new("state", state),
        };

        if (_config.UsePkce)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(codeChallenge);
            query.Add(new("code_challenge", codeChallenge));
            query.Add(new("code_challenge_method", CodeChallengeMethod));
        }

        var queryString = string.Join('&', query.Select(static kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return Task.FromResult($"{_config.AuthorizationEndpoint}?{queryString}");
    }

    /// <inheritdoc />
    public async Task<OAuthToken> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var form = new List<KeyValuePair<string, string>>
        {
            new("client_id", _config.ClientId),
            new("client_secret", _config.ClientSecret),
            new("code", code),
            new("grant_type", "authorization_code"),
            new("redirect_uri", _config.RedirectUri),
        };

        if (_config.UsePkce)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);
            form.Add(new("code_verifier", codeVerifier));
        }

        using var content = new FormUrlEncodedContent(form);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        using var response = await client.PostAsync(_config.TokenEndpoint, content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Google token exchange failed with status {(int)response.StatusCode}: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken).ConfigureAwait(false)
            ?? throw new HttpRequestException("Google token endpoint returned an empty response.");

        var scopes = string.IsNullOrWhiteSpace(payload.Scope)
            ? Array.Empty<string>()
            : payload.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new OAuthToken
        {
            AccessToken = payload.AccessToken,
            TokenType = string.IsNullOrWhiteSpace(payload.TokenType) ? TokenTypes.Bearer : payload.TokenType,
            RefreshToken = payload.RefreshToken,
            ExpiresAt = _timeProvider.GetUtcNow().AddSeconds(payload.ExpiresIn),
            Scopes = scopes,
            IssuedBy = _config.ProviderName,
        };
    }

    /// <inheritdoc />
    public async Task<IAuthUser> GetUserAsync(OAuthToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(token.AccessToken);

        var client = _httpClientFactory.CreateClient(HttpClientName);

        using var request = new HttpRequestMessage(HttpMethod.Get, _config.UserInformationEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            string.IsNullOrWhiteSpace(token.TokenType) ? TokenTypes.Bearer : token.TokenType,
            token.AccessToken);

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Google user-info request failed with status {(int)response.StatusCode}: {body}");
        }

        var info = await response.Content.ReadFromJsonAsync<UserInfoResponse>(cancellationToken).ConfigureAwait(false)
            ?? throw new HttpRequestException("Google user-info endpoint returned an empty response.");

        if (string.IsNullOrWhiteSpace(info.Subject))
            throw new HttpRequestException("Google user-info response is missing the 'sub' field.");

        var displayName = !string.IsNullOrWhiteSpace(info.Name) ? info.Name
            : !string.IsNullOrWhiteSpace(info.Email) ? info.Email
            : info.Subject;
        var email = info.Email ?? string.Empty;

        return new AuthUser(info.Subject, displayName, email, _config.ProviderName);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed record TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
        [JsonPropertyName("token_type")] public string? TokenType { get; init; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
        [JsonPropertyName("scope")] public string? Scope { get; init; }
    }

    private sealed record UserInfoResponse
    {
        [JsonPropertyName("sub")] public string Subject { get; init; } = string.Empty;
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("email")] public string? Email { get; init; }
    }
}
