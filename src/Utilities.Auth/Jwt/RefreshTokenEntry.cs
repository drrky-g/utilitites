namespace Utilities.Auth;

public sealed record RefreshTokenEntry
{
    public required string Token { get; init; }
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
    public required string Email { get; init; }
    public required string ProviderName { get; init; }
    public required IReadOnlyList<string> Scopes { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
