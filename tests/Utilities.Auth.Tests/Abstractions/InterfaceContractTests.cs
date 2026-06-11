using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using FluentAssertions;
using Utilities.Auth;
using Xunit;

namespace Utilities.Auth.Tests.Abstractions;

/// <summary>
/// Reflection-based shape/stability tests that catch accidental renames or member removals.
/// These are intentionally strict — failing here signals a breaking interface change.
/// </summary>
public sealed class InterfaceContractTests
{
    private static readonly Assembly AuthAssembly = typeof(IAuthUser).Assembly;

    // ── IAuthUser ──────────────────────────────────────────────────────────────

    [Fact]
    public void IAuthUser_extends_IPrincipal() =>
        typeof(IAuthUser).GetInterfaces().Should().Contain(typeof(IPrincipal));

    [Theory]
    [InlineData("UserId")]
    [InlineData("DisplayName")]
    [InlineData("Email")]
    [InlineData("ProviderName")]
    [InlineData("ClaimsPrincipal")]
    public void IAuthUser_has_expected_property(string propertyName) =>
        typeof(IAuthUser).GetProperty(propertyName).Should().NotBeNull(
            because: $"{nameof(IAuthUser)}.{propertyName} is part of the stable contract");

    [Fact]
    public void IAuthUser_ClaimsPrincipal_property_returns_ClaimsPrincipal() =>
        typeof(IAuthUser).GetProperty("ClaimsPrincipal")!.PropertyType
            .Should().Be(typeof(ClaimsPrincipal));

    // ── ITokenService ──────────────────────────────────────────────────────────

    [Fact]
    public void ITokenService_extends_IAsyncDisposable() =>
        typeof(ITokenService).GetInterfaces().Should().Contain(typeof(IAsyncDisposable));

    [Theory]
    [InlineData("IssueTokenAsync")]
    [InlineData("ValidateTokenAsync")]
    [InlineData("RefreshTokenAsync")]
    public void ITokenService_has_expected_method(string methodName) =>
        typeof(ITokenService).GetMethod(methodName).Should().NotBeNull(
            because: $"{nameof(ITokenService)}.{methodName} is part of the stable contract");

    // ── IOAuthProvider ─────────────────────────────────────────────────────────

    [Fact]
    public void IOAuthProvider_extends_IOAuthProviderRegistration() =>
        typeof(IOAuthProvider).GetInterfaces().Should().Contain(typeof(IOAuthProviderRegistration));

    [Fact]
    public void IOAuthProvider_extends_IAsyncDisposable() =>
        typeof(IOAuthProvider).GetInterfaces().Should().Contain(typeof(IAsyncDisposable));

    [Theory]
    [InlineData("BuildAuthorizationUrlAsync")]
    [InlineData("ExchangeCodeAsync")]
    [InlineData("GetUserAsync")]
    public void IOAuthProvider_has_expected_method(string methodName) =>
        typeof(IOAuthProvider).GetMethod(methodName).Should().NotBeNull(
            because: $"{nameof(IOAuthProvider)}.{methodName} is part of the stable contract");

    // ── TokenValidationFailureReason ───────────────────────────────────────────

    [Theory]
    [InlineData(nameof(TokenValidationFailureReason.Expired))]
    [InlineData(nameof(TokenValidationFailureReason.InvalidSignature))]
    [InlineData(nameof(TokenValidationFailureReason.InvalidIssuer))]
    [InlineData(nameof(TokenValidationFailureReason.InvalidAudience))]
    [InlineData(nameof(TokenValidationFailureReason.Malformed))]
    [InlineData(nameof(TokenValidationFailureReason.Revoked))]
    [InlineData(nameof(TokenValidationFailureReason.Unknown))]
    public void TokenValidationFailureReason_has_expected_value(string name) =>
        Enum.GetNames<TokenValidationFailureReason>().Should().Contain(name);

    // ── AuthClaimTypes ─────────────────────────────────────────────────────────

    [Fact]
    public void AuthClaimTypes_Provider_constant_is_stable() =>
        AuthClaimTypes.Provider.Should().Be("auth:provider");

    [Fact]
    public void AuthClaimTypes_Scope_constant_is_stable() =>
        AuthClaimTypes.Scope.Should().Be("auth:scope");

    // ── TokenTypes ─────────────────────────────────────────────────────────────

    [Fact]
    public void TokenTypes_Bearer_constant_is_stable() =>
        TokenTypes.Bearer.Should().Be("Bearer");

    // ── OAuthToken ─────────────────────────────────────────────────────────────

    [Fact]
    public void OAuthToken_is_sealed_and_equatable()
    {
        var type = typeof(OAuthToken);
        type.IsSealed.Should().BeTrue();
        type.GetInterfaces().Should().Contain(typeof(IEquatable<OAuthToken>));
    }

    [Theory]
    [InlineData("AccessToken")]
    [InlineData("TokenType")]
    [InlineData("RefreshToken")]
    [InlineData("ExpiresAt")]
    [InlineData("Scopes")]
    [InlineData("IssuedBy")]
    [InlineData("IsExpired")]
    [InlineData("IsRefreshable")]
    public void OAuthToken_has_expected_member(string memberName) =>
        typeof(OAuthToken).GetMember(memberName).Should().NotBeEmpty(
            because: $"{nameof(OAuthToken)}.{memberName} is part of the stable contract");

    // ── TokenValidationResult ──────────────────────────────────────────────────

    [Fact]
    public void TokenValidationResult_is_sealed_record()
    {
        var type = typeof(TokenValidationResult);
        type.IsSealed.Should().BeTrue();
        type.GetMethod("<Clone>$").Should().NotBeNull(because: "records expose a clone method");
    }

    [Theory]
    [InlineData("IsValid")]
    [InlineData("Principal")]
    [InlineData("FailureReason")]
    [InlineData("FailureMessage")]
    [InlineData("ExpiresAt")]
    [InlineData("IsExpired")]
    public void TokenValidationResult_has_expected_member(string memberName) =>
        typeof(TokenValidationResult).GetMember(memberName).Should().NotBeEmpty(
            because: $"{nameof(TokenValidationResult)}.{memberName} is part of the stable contract");

    // ── AuthUserClaims ─────────────────────────────────────────────────────────

    [Fact]
    public void AuthUserClaims_is_sealed() =>
        typeof(AuthUserClaims).IsSealed.Should().BeTrue();

    [Fact]
    public void AuthUserClaims_extends_ClaimsIdentity() =>
        typeof(AuthUserClaims).BaseType.Should().Be(typeof(ClaimsIdentity));
}
