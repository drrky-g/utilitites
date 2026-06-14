using System.Security.Claims;
using System.Security.Principal;

namespace Utilities.Auth;

/// <summary>
/// Concrete <see cref="IAuthUser"/> backed by an <see cref="AuthUserClaims"/> identity. Returned by OAuth
/// providers after mapping a provider's user-info response.
/// </summary>
public sealed class AuthUser : IAuthUser
{
    private readonly ClaimsPrincipal _principal;

    /// <summary>Creates an authenticated user and builds its claims principal from the supplied fields.</summary>
    public AuthUser(string userId, string displayName, string email, string providerName)
    {
        UserId = userId;
        DisplayName = displayName;
        Email = email;
        ProviderName = providerName;
        _principal = new AuthUserClaims(userId, displayName, email, providerName).ToClaimsPrincipal();
    }

    /// <inheritdoc />
    public string UserId { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public string Email { get; }

    /// <inheritdoc />
    public string ProviderName { get; }

    /// <inheritdoc />
    public ClaimsPrincipal ClaimsPrincipal => _principal;

    /// <inheritdoc />
    public IIdentity? Identity => _principal.Identity;

    /// <inheritdoc />
    public bool IsInRole(string role) => _principal.IsInRole(role);
}
