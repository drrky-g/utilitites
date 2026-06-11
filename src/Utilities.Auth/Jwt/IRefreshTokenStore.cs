namespace Utilities.Auth;

public interface IRefreshTokenStore : IAsyncDisposable
{
    Task StoreAsync(RefreshTokenEntry entry, CancellationToken ct = default);
    Task<RefreshTokenEntry?> FindAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(string token, CancellationToken ct = default);
}
