using FluentAssertions;
using Xunit;

namespace Utilities.Auth.Tests.Providers.Google;

public sealed class GoogleOAuthConfigTests
{
    private static GoogleOAuthConfig Valid() => new()
    {
        ClientId = "client-123",
        ClientSecret = "secret-xyz",
        RedirectUri = "https://app.example.com/callback",
    };

    [Fact]
    public void Defaults_target_google_endpoints_and_scopes()
    {
        var config = new GoogleOAuthConfig();

        config.ProviderName.Should().Be("google");
        config.UsePkce.Should().BeTrue();
        config.AuthorizationEndpoint.Should().Be("https://accounts.google.com/o/oauth2/v2/auth");
        config.TokenEndpoint.Should().Be("https://oauth2.googleapis.com/token");
        config.UserInformationEndpoint.Should().Be("https://openidconnect.googleapis.com/v1/userinfo");
        config.Scope.Should().Contain(new[] { "openid", "email", "profile" });
    }

    [Fact]
    public void Validate_succeeds_for_a_well_formed_config()
    {
        var config = Valid();

        config.Validate(null, config).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_when_redirect_uri_missing()
    {
        var config = Valid();
        config.RedirectUri = "";

        var result = config.Validate(null, config);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(GoogleOAuthConfig.RedirectUri));
    }

    [Fact]
    public void Validate_fails_when_client_id_missing()
    {
        var config = Valid();
        config.ClientId = "";

        config.Validate(null, config).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_reports_both_base_and_derived_failures()
    {
        var config = new GoogleOAuthConfig { ClientId = "", ClientSecret = "", RedirectUri = "" };

        var result = config.Validate(null, config);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(GoogleOAuthConfig.RedirectUri));
        result.FailureMessage.Should().Contain("ClientId");
    }
}
