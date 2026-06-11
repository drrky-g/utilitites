using System.Collections.Concurrent;

namespace Utilities.Auth;

public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _store = new();
    private bool _disposed;

    public Task StoreAsync(RefreshTokenEntry entry, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _store[entry.Token] = entry;
        return Task.CompletedTask;
    }

    public Task<RefreshTokenEntry?> FindAsync(string token, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _store.TryGetValue(token, out var entry);
        return Task.FromResult(entry);
    }

    public Task RevokeAsync(string token, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _store.TryRemove(token, out _);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _store.Clear();
        return ValueTask.CompletedTask;
    }
}
