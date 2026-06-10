namespace Utilities.Auth;

/// <summary>Custom claim type constants used throughout the auth library.</summary>
public static class AuthClaimTypes
{
    /// <summary>Claim identifying the OAuth provider that issued the credential.</summary>
    public const string Provider = "auth:provider";

    /// <summary>Claim carrying a granted OAuth scope.</summary>
    public const string Scope = "auth:scope";
}
