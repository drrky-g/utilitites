using System.Security.Claims;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Utilities.Auth.Tests.Jwt;

public sealed class JwtTokenServiceTests
{
    private static string ValidKey32() => Convert.ToBase64String(new byte[32]);

    private static JwtTokenOptions ValidOptions(string issuer = "test-issuer", string? audience = null) => new()
    {
        SigningKeyBase64 = ValidKey32(),
        Issuer = issuer,
        Audience = audience,
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
        RefreshTokenLifetime = TimeSpan.FromDays(7),
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    private static (JwtTokenService svc, InMemoryRefreshTokenStore store, FakeTimeProvider clock) BuildSut(
        JwtTokenOptions? options = null, string? audience = null)
    {
        var clock = new FakeTimeProvider();
        var store = new InMemoryRefreshTokenStore();
        var svc = new JwtTokenService(options ?? ValidOptions(audience: audience), store, clock);
        return (svc, store, clock);
    }

    private static IAuthUser MakeUser(params string[] scopes)
    {
        var identity = new AuthUserClaims("user-1", "Test User", "test@example.com", "test-provider");
        foreach (var scope in scopes)
            identity.AddClaim(new Claim(AuthClaimTypes.Scope, scope));
        return new SimpleAuthUser("user-1", "Test User", "test@example.com", "test-provider", identity.ToClaimsPrincipal());
    }

    private sealed class SimpleAuthUser(
        string userId, string displayName, string email, string providerName, ClaimsPrincipal principal)
        : IAuthUser
    {
        public string UserId { get; } = userId;
        public string DisplayName { get; } = displayName;
        public string Email { get; } = email;
        public string ProviderName { get; } = providerName;
        public ClaimsPrincipal ClaimsPrincipal { get; } = principal;
        public System.Security.Principal.IIdentity? Identity => ClaimsPrincipal.Identity;
        public bool IsInRole(string role) => ClaimsPrincipal.IsInRole(role);
    }

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_throws_for_null_options()
    {
        var act = () => new JwtTokenService(null!, new InMemoryRefreshTokenStore());
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_throws_for_null_store()
    {
        var act = () => new JwtTokenService(ValidOptions(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public void Constructor_throws_for_invalid_options()
    {
        var bad = new JwtTokenOptions { SigningKeyBase64 = "" };
        var act = () => new JwtTokenService(bad, new InMemoryRefreshTokenStore());
        act.Should().Throw<ArgumentException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_throws_when_key_too_short()
    {
        var bad = ValidOptions();
        bad.SigningKeyBase64 = Convert.ToBase64String(new byte[16]);
        var act = () => new JwtTokenService(bad, new InMemoryRefreshTokenStore());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_without_timeProvider_uses_system_clock()
    {
        var svc = new JwtTokenService(ValidOptions(), new InMemoryRefreshTokenStore());
        svc.Should().NotBeNull();
    }

    // ── IssueTokenAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task IssueToken_returns_bearer_OAuthToken()
    {
        var (svc, _, _) = BuildSut();
        var token = await svc.IssueTokenAsync(MakeUser());
        token.TokenType.Should().Be(TokenTypes.Bearer);
        token.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task IssueToken_ExpiresAt_matches_access_token_lifetime()
    {
        var (svc, _, clock) = BuildSut();
        var start = clock.GetUtcNow();
        var token = await svc.IssueTokenAsync(MakeUser());
        token.ExpiresAt.Should().BeCloseTo(start + TimeSpan.FromMinutes(15), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task IssueToken_IssuedBy_matches_issuer()
    {
        var (svc, _, _) = BuildSut(ValidOptions("my-issuer"));
        var token = await svc.IssueTokenAsync(MakeUser());
        token.IssuedBy.Should().Be("my-issuer");
    }

    [Fact]
    public async Task IssueToken_scopes_preserved()
    {
        var (svc, _, _) = BuildSut();
        var token = await svc.IssueTokenAsync(MakeUser("read", "write"));
        token.Scopes.Should().BeEquivalentTo(new[] { "read", "write" });
    }

    [Fact]
    public async Task IssueToken_access_token_has_three_segments()
    {
        var (svc, _, _) = BuildSut();
        var token = await svc.IssueTokenAsync(MakeUser());
        token.AccessToken.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public async Task IssueToken_refresh_token_is_stored()
    {
        var (svc, store, _) = BuildSut();
        var token = await svc.IssueTokenAsync(MakeUser());
        var entry = await store.FindAsync(token.RefreshToken!);
        entry.Should().NotBeNull();
        entry!.UserId.Should().Be("user-1");
    }

    // ── ValidateTokenAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateToken_success_returns_correct_claims()
    {
        var (svc, _, _) = BuildSut();
        var issued = await svc.IssueTokenAsync(MakeUser("openid"));
        var result = await svc.ValidateTokenAsync(issued.AccessToken);
        result.IsValid.Should().BeTrue();
        var principal = result.Principal!;
        principal.FindFirstValue("sub").Should().Be("user-1");
        principal.FindFirstValue("email").Should().Be("test@example.com");
        principal.FindFirstValue(AuthClaimTypes.Provider).Should().Be("test-provider");
        principal.FindAll(AuthClaimTypes.Scope).Should().ContainSingle(c => c.Value == "openid");
    }

    [Fact]
    public async Task ValidateToken_success_returns_correct_expiresAt()
    {
        var (svc, _, clock) = BuildSut();
        var start = clock.GetUtcNow();
        var issued = await svc.IssueTokenAsync(MakeUser());
        var result = await svc.ValidateTokenAsync(issued.AccessToken);
        result.IsValid.Should().BeTrue();
        result.ExpiresAt.Should().BeCloseTo(start + TimeSpan.FromMinutes(15), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ValidateToken_returns_expired_after_lifetime_and_skew()
    {
        var (svc, _, clock) = BuildSut();
        var issued = await svc.IssueTokenAsync(MakeUser());
        clock.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(31));
        var result = await svc.ValidateTokenAsync(issued.AccessToken);
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(TokenValidationFailureReason.Expired);
    }

    [Fact]
    public async Task ValidateToken_still_valid_within_clock_skew()
    {
        var (svc, _, clock) = BuildSut();
        var issued = await svc.IssueTokenAsync(MakeUser());
        clock.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(10));
        var result = await svc.ValidateTokenAsync(issued.AccessToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToken_returns_invalid_signature_for_tampered_token()
    {
        var (svc, _, _) = BuildSut();
        var issued = await svc.IssueTokenAsync(MakeUser());
        var parts = issued.AccessToken.Split('.');
        var sig = parts[2];
        parts[2] = sig[..^1] + (sig[^1] == 'A' ? 'B' : 'A');
        var tampered = string.Join(".", parts);
        var result = await svc.ValidateTokenAsync(tampered);
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(TokenValidationFailureReason.InvalidSignature);
    }

    [Fact]
    public async Task ValidateToken_returns_invalid_issuer_for_wrong_issuer()
    {
        var (svc, _, _) = BuildSut();
        var issued = await svc.IssueTokenAsync(MakeUser());
        var wrongOpts = ValidOptions("other-issuer");
        var wrongSvc = new JwtTokenService(wrongOpts, new InMemoryRefreshTokenStore());
        var result = await wrongSvc.ValidateTokenAsync(issued.AccessToken);
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(TokenValidationFailureReason.InvalidIssuer);
    }

    [Fact]
    public async Task ValidateToken_returns_invalid_audience_for_wrong_audience()
    {
        var opts = ValidOptions(audience: "aud-A");
        var (svc, _, _) = BuildSut(opts);
        var issued = await svc.IssueTokenAsync(MakeUser());
        var wrongOpts = ValidOptions(audience: "aud-B");
        var wrongSvc = new JwtTokenService(wrongOpts, new InMemoryRefreshTokenStore());
        var result = await wrongSvc.ValidateTokenAsync(issued.AccessToken);
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(TokenValidationFailureReason.InvalidAudience);
    }

    [Fact]
    public async Task ValidateToken_returns_malformed_for_empty_string()
    {
        var (svc, _, _) = BuildSut();
        var result = await svc.ValidateTokenAsync("");
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(TokenValidationFailureReason.Malformed);
    }

    [Fact]
    public async Task ValidateToken_returns_malformed_for_non_jwt_string()
    {
        var (svc, _, _) = BuildSut();
        var result = await svc.ValidateTokenAsync("not-a-jwt");
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().NotBe(TokenValidationFailureReason.Expired);
    }

    [Fact]
    public async Task ValidateToken_succeeds_without_audience_when_audience_null()
    {
        var opts = ValidOptions(audience: null);
        var (svc, _, _) = BuildSut(opts);
        var issued = await svc.IssueTokenAsync(MakeUser());
        var result = await svc.ValidateTokenAsync(issued.AccessToken);
        result.IsValid.Should().BeTrue();
    }

    // ── RefreshTokenAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_returns_new_valid_OAuthToken()
    {
        var (svc, _, _) = BuildSut();
        var first = await svc.IssueTokenAsync(MakeUser());
        var refreshed = await svc.RefreshTokenAsync(first.RefreshToken!);
        refreshed.AccessToken.Should().NotBeNullOrEmpty();
        refreshed.TokenType.Should().Be(TokenTypes.Bearer);
    }

    [Fact]
    public async Task RefreshToken_new_access_token_validates()
    {
        var (svc, _, _) = BuildSut();
        var first = await svc.IssueTokenAsync(MakeUser());
        var refreshed = await svc.RefreshTokenAsync(first.RefreshToken!);
        var result = await svc.ValidateTokenAsync(refreshed.AccessToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_old_refresh_token_is_revoked()
    {
        var (svc, store, _) = BuildSut();
        var first = await svc.IssueTokenAsync(MakeUser());
        var oldRefresh = first.RefreshToken!;
        await svc.RefreshTokenAsync(oldRefresh);
        var entry = await store.FindAsync(oldRefresh);
        entry.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_new_refresh_token_differs_from_old()
    {
        var (svc, _, _) = BuildSut();
        var first = await svc.IssueTokenAsync(MakeUser());
        var refreshed = await svc.RefreshTokenAsync(first.RefreshToken!);
        refreshed.RefreshToken.Should().NotBe(first.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_carries_forward_userId_and_scopes()
    {
        var (svc, _, _) = BuildSut();
        var first = await svc.IssueTokenAsync(MakeUser("profile", "email"));
        var refreshed = await svc.RefreshTokenAsync(first.RefreshToken!);
        var result = await svc.ValidateTokenAsync(refreshed.AccessToken);
        result.IsValid.Should().BeTrue();
        var principal = result.Principal!;
        principal.FindFirstValue("sub").Should().Be("user-1");
        principal.FindAll(AuthClaimTypes.Scope).Select(c => c.Value)
            .Should().BeEquivalentTo(new[] { "profile", "email" });
    }

    [Fact]
    public async Task RefreshToken_throws_SecurityTokenException_when_not_found()
    {
        var (svc, _, _) = BuildSut();
        var act = async () => await svc.RefreshTokenAsync("non-existent-token");
        await act.Should().ThrowAsync<SecurityTokenException>();
    }

    [Fact]
    public async Task RefreshToken_throws_SecurityTokenExpiredException_when_expired()
    {
        var (svc, _, clock) = BuildSut();
        var first = await svc.IssueTokenAsync(MakeUser());
        clock.Advance(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1));
        var act = async () => await svc.RefreshTokenAsync(first.RefreshToken!);
        await act.Should().ThrowAsync<SecurityTokenExpiredException>();
    }

    [Fact]
    public async Task RefreshToken_revokes_expired_entry_before_throwing()
    {
        var (svc, store, clock) = BuildSut();
        var first = await svc.IssueTokenAsync(MakeUser());
        var oldRefresh = first.RefreshToken!;
        clock.Advance(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1));
        try { await svc.RefreshTokenAsync(oldRefresh); } catch { /* expected */ }
        var entry = await store.FindAsync(oldRefresh);
        entry.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_throws_ArgumentException_for_empty_input()
    {
        var (svc, _, _) = BuildSut();
        var act = async () => await svc.RefreshTokenAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── DisposeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_delegates_to_store()
    {
        var mockStore = Substitute.For<IRefreshTokenStore>();
        mockStore.DisposeAsync().Returns(ValueTask.CompletedTask);
        var svc = new JwtTokenService(ValidOptions(), mockStore);
        await svc.DisposeAsync();
        await mockStore.Received(1).DisposeAsync();
    }
}
