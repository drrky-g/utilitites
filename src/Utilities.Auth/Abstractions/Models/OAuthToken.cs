using Microsoft.AspNetCore.Authentication;

namespace Utilities.Auth;

/// <summary>Immutable value object representing a token returned from an OAuth provider or issued by this library.</summary>
public sealed class OAuthToken : AuthenticationToken, IEquatable<OAuthToken>
{
    /// <summary>The raw access token string.</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>The token type (e.g. <see cref="TokenTypes.Bearer"/>).</summary>
    public string TokenType { get; init; } = TokenTypes.Bearer;

    /// <summary>An optional refresh token for obtaining new access tokens.</summary>
    public string? RefreshToken { get; init; }

    /// <summary>The UTC time at which this access token expires.</summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>The scopes granted with this token.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();

    /// <summary>The issuer identifier — either the OAuth provider name or this service's identifier.</summary>
    public string IssuedBy { get; init; } = string.Empty;

    /// <summary>True when the access token's expiry has passed.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>True when a refresh token is present.</summary>
    public bool IsRefreshable => !string.IsNullOrEmpty(RefreshToken);

    /// <inheritdoc />
    public bool Equals(OAuthToken? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return AccessToken == other.AccessToken
            && TokenType == other.TokenType
            && RefreshToken == other.RefreshToken
            && ExpiresAt == other.ExpiresAt
            && Scopes.SequenceEqual(other.Scopes)
            && IssuedBy == other.IssuedBy
            && Name == other.Name
            && Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as OAuthToken);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(AccessToken, TokenType, RefreshToken, ExpiresAt, IssuedBy, Name, Value);

    /// <inheritdoc />
    public static bool operator ==(OAuthToken? left, OAuthToken? right) => Equals(left, right);

    /// <inheritdoc />
    public static bool operator !=(OAuthToken? left, OAuthToken? right) => !Equals(left, right);
}
