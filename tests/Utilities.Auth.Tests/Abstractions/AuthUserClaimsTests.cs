using System.Security.Claims;
using FluentAssertions;
using Utilities.Auth;
using Xunit;

namespace Utilities.Auth.Tests.Abstractions;

public sealed class AuthUserClaimsTests
{
    private const string UserId = "u-001";
    private const string DisplayName = "Jane Doe";
    private const string Email = "jane@example.com";
    private const string Provider = "google";

    [Fact]
    public void Constructor_populates_NameIdentifier_claim()
    {
        var identity = new AuthUserClaims(UserId, DisplayName, Email, Provider);

        identity.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(UserId);
    }

    [Fact]
    public void Constructor_populates_Name_claim()
    {
        var identity = new AuthUserClaims(UserId, DisplayName, Email, Provider);

        identity.FindFirst(ClaimTypes.Name)!.Value.Should().Be(DisplayName);
    }

    [Fact]
    public void Constructor_populates_Email_claim()
    {
        var identity = new AuthUserClaims(UserId, DisplayName, Email, Provider);

        identity.FindFirst(ClaimTypes.Email)!.Value.Should().Be(Email);
    }

    [Fact]
    public void Constructor_populates_Provider_claim()
    {
        var identity = new AuthUserClaims(UserId, DisplayName, Email, Provider);

        identity.FindFirst(AuthClaimTypes.Provider)!.Value.Should().Be(Provider);
    }

    [Fact]
    public void AuthenticationType_matches_providerName()
    {
        var identity = new AuthUserClaims(UserId, DisplayName, Email, Provider);

        identity.AuthenticationType.Should().Be(Provider);
    }

    [Fact]
    public void ToClaimsPrincipal_wraps_identity()
    {
        var identity = new AuthUserClaims(UserId, DisplayName, Email, Provider);
        var principal = identity.ToClaimsPrincipal();

        principal.Identities.Should().ContainSingle().Which.Should().BeSameAs(identity);
    }

    [Theory]
    [InlineData("", DisplayName, Email, Provider, nameof(UserId))]
    [InlineData(UserId, "", Email, Provider, nameof(DisplayName))]
    [InlineData(UserId, DisplayName, "", Provider, nameof(Email))]
    [InlineData(UserId, DisplayName, Email, "", nameof(Provider))]
    public void Constructor_throws_on_empty_argument(string userId, string displayName, string email, string provider, string _)
    {
        var act = () => new AuthUserClaims(userId, displayName, email, provider);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_throws_on_null_userId()
    {
        var act = () => new AuthUserClaims(null!, DisplayName, Email, Provider);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_throws_on_null_displayName()
    {
        var act = () => new AuthUserClaims(UserId, null!, Email, Provider);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_throws_on_null_email()
    {
        var act = () => new AuthUserClaims(UserId, DisplayName, null!, Provider);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_throws_on_null_provider()
    {
        var act = () => new AuthUserClaims(UserId, DisplayName, Email, null!);
        act.Should().Throw<ArgumentException>();
    }
}
