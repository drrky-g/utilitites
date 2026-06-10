using FluentAssertions;
using Utilities.Auth;
using Xunit;

namespace Utilities.Auth.Tests.Abstractions;

public sealed class OAuthTokenTests
{
    [Fact]
    public void Defaults_are_sane()
    {
        var token = new OAuthToken();

        token.AccessToken.Should().Be(string.Empty);
        token.TokenType.Should().Be(TokenTypes.Bearer);
        token.RefreshToken.Should().BeNull();
        token.Scopes.Should().BeEmpty();
        token.IssuedBy.Should().Be(string.Empty);
    }

    [Fact]
    public void IsExpired_false_when_expiry_is_in_future()
    {
        var token = new OAuthToken { ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_true_when_expiry_is_in_past()
    {
        var token = new OAuthToken { ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) };

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsRefreshable_true_when_refresh_token_present()
    {
        var token = new OAuthToken { RefreshToken = "rt_abc123", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };

        token.IsRefreshable.Should().BeTrue();
    }

    [Fact]
    public void IsRefreshable_true_even_when_access_token_expired()
    {
        var token = new OAuthToken { RefreshToken = "rt_abc123", ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) };

        token.IsRefreshable.Should().BeTrue();
    }

    [Fact]
    public void IsRefreshable_false_when_no_refresh_token()
    {
        var token = new OAuthToken { RefreshToken = null };

        token.IsRefreshable.Should().BeFalse();
    }

    [Fact]
    public void Value_equality_matches_on_all_init_properties()
    {
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var a = new OAuthToken { AccessToken = "tok", RefreshToken = "rt", ExpiresAt = expiry, IssuedBy = "google" };
        var b = new OAuthToken { AccessToken = "tok", RefreshToken = "rt", ExpiresAt = expiry, IssuedBy = "google" };

        a.Should().Be(b);
    }

    [Fact]
    public void Value_inequality_when_any_property_differs()
    {
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var a = new OAuthToken { AccessToken = "tok", ExpiresAt = expiry };
        var b = new OAuthToken { AccessToken = "different", ExpiresAt = expiry };

        a.Should().NotBe(b);
    }
}
