using System.Security.Claims;

namespace Utilities.Auth;

/// <summary>
/// A <see cref="ClaimsIdentity"/> pre-populated with the standard claims for an OAuth-authenticated user.
/// </summary>
public sealed class AuthUserClaims : ClaimsIdentity
{
    /// <summary>
    /// Constructs an identity and populates NameIdentifier, Name, Email, and Provider claims.
    /// </summary>
    public AuthUserClaims(string userId, string displayName, string email, string providerName)
        : base(authenticationType: providerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(displayName);
        ArgumentException.ThrowIfNullOrEmpty(email);
        ArgumentException.ThrowIfNullOrEmpty(providerName);

        AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        AddClaim(new Claim(ClaimTypes.Name, displayName));
        AddClaim(new Claim(ClaimTypes.Email, email));
        AddClaim(new Claim(AuthClaimTypes.Provider, providerName));
    }

    /// <summary>Wraps this identity in a <see cref="ClaimsPrincipal"/>.</summary>
    public ClaimsPrincipal ToClaimsPrincipal() => new(this);
}
