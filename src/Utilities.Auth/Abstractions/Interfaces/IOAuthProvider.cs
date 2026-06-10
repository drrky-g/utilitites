namespace Utilities.Auth;

/// <summary>Encapsulates the OAuth authorization flow for a specific provider.</summary>
public interface IOAuthProvider : IOAuthProviderRegistration, IAsyncDisposable
{
    /// <summary>Builds the provider authorization URL, including PKCE challenge when PKCE is enabled on the provider config.</summary>
    Task<string> BuildAuthorizationUrlAsync(string state, string codeChallenge, CancellationToken cancellationToken = default);

    /// <summary>Exchanges an authorization code for an <see cref="OAuthToken"/>.</summary>
    Task<OAuthToken> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the authenticated user from the provider using the supplied token.</summary>
    Task<IAuthUser> GetUserAsync(OAuthToken token, CancellationToken cancellationToken = default);
}
