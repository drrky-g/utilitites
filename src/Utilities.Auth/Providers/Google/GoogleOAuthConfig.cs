using Microsoft.Extensions.Options;
using Utilities.Auth.Abstractions.Options;

namespace Utilities.Auth;

/// <summary>
/// Configuration for the Google OAuth provider. Pre-populated with Google's endpoints, default scopes, and PKCE
/// enabled; supply <c>ClientId</c>, <c>ClientSecret</c>, and <see cref="RedirectUri"/> per consuming service.
/// </summary>
public sealed class GoogleOAuthConfig : OAuthProviderConfig<GoogleOAuthConfig>
{
    /// <summary>Creates a config with Google's well-known endpoints and the standard OpenID Connect scopes.</summary>
    public GoogleOAuthConfig()
    {
        ProviderName = "google";
        UsePkce = true;
        AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        TokenEndpoint = "https://oauth2.googleapis.com/token";
        UserInformationEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
        Scope.Add("openid");
        Scope.Add("email");
        Scope.Add("profile");
    }

    /// <summary>The redirect URI registered with Google that the authorization response is returned to.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <inheritdoc />
    public override ValidateOptionsResult Validate(string? name, GoogleOAuthConfig options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var baseResult = base.Validate(name, options);
        var errors = baseResult.Failed ? new List<string>(baseResult.Failures!) : new List<string>();

        if (string.IsNullOrWhiteSpace(options.RedirectUri))
            errors.Add($"{nameof(RedirectUri)} is required.");

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
