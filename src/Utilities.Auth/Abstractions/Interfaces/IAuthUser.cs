using System.Security.Claims;
using System.Security.Principal;

namespace Utilities.Auth;

/// <summary>Represents an authenticated user returned from an OAuth provider.</summary>
public interface IAuthUser : IPrincipal
{
    /// <summary>The provider-scoped unique identifier for the user.</summary>
    string UserId { get; }

    /// <summary>The human-readable display name of the user.</summary>
    string DisplayName { get; }

    /// <summary>The user's email address.</summary>
    string Email { get; }

    /// <summary>The name of the OAuth provider that authenticated this user.</summary>
    string ProviderName { get; }

    /// <summary>The full claims principal for this user.</summary>
    ClaimsPrincipal ClaimsPrincipal { get; }
}
