using FluentAssertions;
using Xunit;

namespace Utilities.Auth.Tests.Jwt;

public sealed class InMemoryRefreshTokenStoreTests : IAsyncDisposable
{
    private readonly InMemoryRefreshTokenStore _sut = new();

    public ValueTask DisposeAsync() => _sut.DisposeAsync();

    private static RefreshTokenEntry MakeEntry(string token = "tok-1") => new()
    {
        Token = token,
        UserId = "u-1",
        DisplayName = "Alice",
        Email = "alice@example.com",
        ProviderName = "google",
        Scopes = Array.Empty<string>(),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
    };

    [Fact]
    public async Task Store_then_find_returns_entry()
    {
        var entry = MakeEntry();
        await _sut.StoreAsync(entry);
        var found = await _sut.FindAsync(entry.Token);
        found.Should().Be(entry);
    }

    [Fact]
    public async Task Find_returns_null_for_unknown_token()
    {
        var found = await _sut.FindAsync("does-not-exist");
        found.Should().BeNull();
    }

    [Fact]
    public async Task Revoke_removes_entry()
    {
        var entry = MakeEntry();
        await _sut.StoreAsync(entry);
        await _sut.RevokeAsync(entry.Token);
        var found = await _sut.FindAsync(entry.Token);
        found.Should().BeNull();
    }

    [Fact]
    public async Task Revoke_is_noop_for_unknown_token()
    {
        var act = async () => await _sut.RevokeAsync("never-stored");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Store_overwrites_existing_entry()
    {
        var first = MakeEntry("tok-x");
        var second = first with { DisplayName = "Bob" };
        await _sut.StoreAsync(first);
        await _sut.StoreAsync(second);
        var found = await _sut.FindAsync("tok-x");
        found!.DisplayName.Should().Be("Bob");
    }

    [Fact]
    public async Task Dispose_then_store_throws_ObjectDisposedException()
    {
        await _sut.DisposeAsync();
        var act = async () => await _sut.StoreAsync(MakeEntry());
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Dispose_then_find_throws_ObjectDisposedException()
    {
        await _sut.DisposeAsync();
        var act = async () => await _sut.FindAsync("tok-1");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Dispose_then_revoke_throws_ObjectDisposedException()
    {
        await _sut.DisposeAsync();
        var act = async () => await _sut.RevokeAsync("tok-1");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}
