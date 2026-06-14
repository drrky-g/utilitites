using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Utilities.Auth.Tests.Providers.Google;

public sealed class GoogleOAuthProviderTests
{
    private const string RedirectUri = "https://app.example.com/callback";

    private static GoogleOAuthConfig ValidConfig() => new()
    {
        ClientId = "client-123",
        ClientSecret = "secret-xyz",
        RedirectUri = RedirectUri,
    };

    private static IHttpClientFactory FactoryFor(StubHttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);
        return factory;
    }

    private static GoogleOAuthProvider BuildProvider(
        IHttpClientFactory factory,
        Action<GoogleOAuthConfig>? configure = null,
        TimeProvider? clock = null)
    {
        var config = ValidConfig();
        configure?.Invoke(config);
        return new GoogleOAuthProvider(factory, Options.Create(config), clock);
    }

    // ── Construction ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_throws_for_null_factory()
    {
        var act = () => new GoogleOAuthProvider(null!, Options.Create(ValidConfig()));
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");
    }

    [Fact]
    public void Constructor_throws_for_null_options()
    {
        var act = () => new GoogleOAuthProvider(Substitute.For<IHttpClientFactory>(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    // ── BuildAuthorizationUrlAsync ─────────────────────────────────────────────

    [Fact]
    public async Task BuildAuthorizationUrl_includes_required_params_and_pkce()
    {
        var provider = BuildProvider(Substitute.For<IHttpClientFactory>());

        var url = await provider.BuildAuthorizationUrlAsync("state-abc", "challenge-xyz");

        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        url.Should().Contain("client_id=client-123");
        url.Should().Contain("redirect_uri=" + Uri.EscapeDataString(RedirectUri));
        url.Should().Contain("response_type=code");
        url.Should().Contain("state=state-abc");
        url.Should().Contain("scope=" + Uri.EscapeDataString("openid email profile"));
        url.Should().Contain("code_challenge=challenge-xyz");
        url.Should().Contain("code_challenge_method=S256");
    }

    [Fact]
    public async Task BuildAuthorizationUrl_omits_pkce_when_disabled()
    {
        var provider = BuildProvider(Substitute.For<IHttpClientFactory>(), cfg => cfg.UsePkce = false);

        var url = await provider.BuildAuthorizationUrlAsync("state-abc", codeChallenge: "");

        url.Should().NotContain("code_challenge");
    }

    [Fact]
    public async Task BuildAuthorizationUrl_requires_challenge_when_pkce_enabled()
    {
        var provider = BuildProvider(Substitute.For<IHttpClientFactory>());

        var act = () => provider.BuildAuthorizationUrlAsync("state-abc", codeChallenge: "");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BuildAuthorizationUrl_throws_on_blank_state()
    {
        var provider = BuildProvider(Substitute.For<IHttpClientFactory>());

        var act = () => provider.BuildAuthorizationUrlAsync("", "challenge");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── ExchangeCodeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExchangeCode_maps_token_response()
    {
        const string json = """
            {"access_token":"at-123","token_type":"Bearer","refresh_token":"rt-456","expires_in":3599,"scope":"openid email"}
            """;
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, json);
        var clock = new FakeTimeProvider();
        var provider = BuildProvider(FactoryFor(handler), clock: clock);

        var token = await provider.ExchangeCodeAsync("auth-code", "verifier-abc");

        token.AccessToken.Should().Be("at-123");
        token.TokenType.Should().Be("Bearer");
        token.RefreshToken.Should().Be("rt-456");
        token.Scopes.Should().BeEquivalentTo(new[] { "openid", "email" });
        token.IssuedBy.Should().Be("google");
        token.ExpiresAt.Should().Be(clock.GetUtcNow().AddSeconds(3599));
    }

    [Fact]
    public async Task ExchangeCode_posts_expected_form_to_token_endpoint()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"access_token":"at","expires_in":60}""");
        var provider = BuildProvider(FactoryFor(handler));

        await provider.ExchangeCodeAsync("auth-code", "verifier-abc");

        handler.Requests[0].RequestUri.Should().Be(new Uri("https://oauth2.googleapis.com/token"));
        var body = handler.RequestBodies[0]!;
        body.Should().Contain("grant_type=authorization_code");
        body.Should().Contain("code=auth-code");
        body.Should().Contain("client_id=client-123");
        body.Should().Contain("code_verifier=verifier-abc");
    }

    [Fact]
    public async Task ExchangeCode_omits_code_verifier_when_pkce_disabled()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"access_token":"at","expires_in":60}""");
        var provider = BuildProvider(FactoryFor(handler), cfg => cfg.UsePkce = false);

        await provider.ExchangeCodeAsync("auth-code", codeVerifier: "");

        handler.RequestBodies[0]!.Should().NotContain("code_verifier");
    }

    [Fact]
    public async Task ExchangeCode_throws_on_error_response()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");
        var provider = BuildProvider(FactoryFor(handler));

        var act = () => provider.ExchangeCodeAsync("bad-code", "verifier");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── GetUserAsync ───────────────────────────────────────────────────────────

    private static OAuthToken BearerToken(string accessToken = "at-1") =>
        new() { AccessToken = accessToken, TokenType = TokenTypes.Bearer, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) };

    [Fact]
    public async Task GetUser_maps_userinfo_to_auth_user()
    {
        const string json = """{"sub":"google-uid-1","name":"Ada Lovelace","email":"ada@example.com"}""";
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, json);
        var provider = BuildProvider(FactoryFor(handler));

        var user = await provider.GetUserAsync(BearerToken());

        user.UserId.Should().Be("google-uid-1");
        user.DisplayName.Should().Be("Ada Lovelace");
        user.Email.Should().Be("ada@example.com");
        user.ProviderName.Should().Be("google");
        user.ClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("google-uid-1");
        user.ClaimsPrincipal.FindFirst(AuthClaimTypes.Provider)!.Value.Should().Be("google");
    }

    [Fact]
    public async Task GetUser_sends_bearer_authorization_header()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"sub":"uid","email":"a@b.com","name":"A"}""");
        var provider = BuildProvider(FactoryFor(handler));

        await provider.GetUserAsync(BearerToken("token-value"));

        handler.Requests[0].RequestUri.Should().Be(new Uri("https://openidconnect.googleapis.com/v1/userinfo"));
        handler.Requests[0].Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Requests[0].Headers.Authorization!.Parameter.Should().Be("token-value");
    }

    [Fact]
    public async Task GetUser_falls_back_to_email_when_name_missing()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"sub":"uid","email":"a@b.com"}""");
        var provider = BuildProvider(FactoryFor(handler));

        var user = await provider.GetUserAsync(BearerToken());

        user.DisplayName.Should().Be("a@b.com");
    }

    [Fact]
    public async Task GetUser_throws_when_sub_missing()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"name":"No Sub","email":"a@b.com"}""");
        var provider = BuildProvider(FactoryFor(handler));

        var act = () => provider.GetUserAsync(BearerToken());

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetUser_throws_on_error_response()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, """{"error":"invalid_token"}""");
        var provider = BuildProvider(FactoryFor(handler));

        var act = () => provider.GetUserAsync(BearerToken());

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
