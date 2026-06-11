using System.Security.Claims;

namespace Utilities.Auth;

/// <summary>Immutable result from a token validation attempt.</summary>
public sealed record TokenValidationResult
{
    /// <summary>True when the token passed all validation checks.</summary>
    public bool IsValid { get; init; }

    /// <summary>The claims principal extracted from a valid token; null on failure.</summary>
    public ClaimsPrincipal? Principal { get; init; }

    /// <summary>The structured reason for failure; null on success.</summary>
    public TokenValidationFailureReason? FailureReason { get; init; }

    /// <summary>A human-readable failure description; null on success.</summary>
    public string? FailureMessage { get; init; }

    /// <summary>The expiry time embedded in the token, if available.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>True when <see cref="ExpiresAt"/> is set and has already passed.</summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value;

    /// <summary>Creates a successful validation result.</summary>
    public static TokenValidationResult Success(ClaimsPrincipal principal, DateTimeOffset? expiresAt = null) =>
        new() { IsValid = true, Principal = principal, ExpiresAt = expiresAt };

    /// <summary>Creates a failed validation result.</summary>
    public static TokenValidationResult Failure(TokenValidationFailureReason reason, string? message = null) =>
        new() { IsValid = false, FailureReason = reason, FailureMessage = message };
}
