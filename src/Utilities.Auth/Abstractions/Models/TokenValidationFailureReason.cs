namespace Utilities.Auth;

/// <summary>Categorizes why a token failed validation.</summary>
public enum TokenValidationFailureReason
{
    Expired,
    InvalidSignature,
    InvalidIssuer,
    InvalidAudience,
    Malformed,
    Revoked,
    Unknown,
}
