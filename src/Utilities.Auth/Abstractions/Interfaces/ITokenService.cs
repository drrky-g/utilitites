namespace Utilities.Auth;

/// <summary>Issues, validates, and refreshes tokens for authenticated users.</summary>
public interface ITokenService : IAsyncDisposable
{
    /// <summary>Issues a new token for the given user.</summary>
    Task<OAuthToken> IssueTokenAsync(IAuthUser user, CancellationToken cancellationToken = default);

    /// <summary>Validates a raw token string and returns the validation result.</summary>
    Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Exchanges a refresh token for a new access token.</summary>
    Task<OAuthToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
