using FluentAssertions;
using Xunit;

namespace Utilities.Auth.Tests.Jwt;

public sealed class JwtTokenOptionsTests
{
    private static JwtTokenOptions ValidOptions() => new()
    {
        SigningKeyBase64 = Convert.ToBase64String(new byte[32]),
        Issuer = "test",
        Audience = null,
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
        RefreshTokenLifetime = TimeSpan.FromDays(7),
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    [Fact]
    public void Defaults_are_correct()
    {
        var o = new JwtTokenOptions();
        o.SigningKeyBase64.Should().Be("");
        o.Issuer.Should().Be("Utilities.Auth");
        o.Audience.Should().BeNull();
        o.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(15));
        o.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(7));
        o.ClockSkew.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Validate_succeeds_for_valid_config()
    {
        var o = ValidOptions();
        var result = o.Validate(null, o);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_for_empty_key()
    {
        var o = ValidOptions();
        o.SigningKeyBase64 = "";
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*SigningKeyBase64*");
    }

    [Fact]
    public void Validate_fails_for_invalid_base64()
    {
        var o = ValidOptions();
        o.SigningKeyBase64 = "not-valid-base64!!!";
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*Base64*");
    }

    [Fact]
    public void Validate_fails_when_key_too_short()
    {
        var o = ValidOptions();
        o.SigningKeyBase64 = Convert.ToBase64String(new byte[16]);
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*32 bytes*");
    }

    [Fact]
    public void Validate_accepts_key_exactly_32_bytes()
    {
        var o = ValidOptions();
        o.SigningKeyBase64 = Convert.ToBase64String(new byte[32]);
        o.Validate(null, o).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_for_zero_access_token_lifetime()
    {
        var o = ValidOptions();
        o.AccessTokenLifetime = TimeSpan.Zero;
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*AccessTokenLifetime*");
    }

    [Fact]
    public void Validate_fails_for_negative_access_token_lifetime()
    {
        var o = ValidOptions();
        o.AccessTokenLifetime = TimeSpan.FromMinutes(-1);
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_for_zero_refresh_token_lifetime()
    {
        var o = ValidOptions();
        o.RefreshTokenLifetime = TimeSpan.Zero;
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*RefreshTokenLifetime*");
    }

    [Fact]
    public void Validate_fails_for_negative_refresh_token_lifetime()
    {
        var o = ValidOptions();
        o.RefreshTokenLifetime = TimeSpan.FromDays(-1);
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_for_negative_clock_skew()
    {
        var o = ValidOptions();
        o.ClockSkew = TimeSpan.FromSeconds(-1);
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainMatch("*ClockSkew*");
    }

    [Fact]
    public void Validate_accepts_zero_clock_skew()
    {
        var o = ValidOptions();
        o.ClockSkew = TimeSpan.Zero;
        o.Validate(null, o).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_collects_multiple_failures()
    {
        var o = new JwtTokenOptions
        {
            SigningKeyBase64 = "",
            AccessTokenLifetime = TimeSpan.Zero,
            RefreshTokenLifetime = TimeSpan.Zero,
        };
        var result = o.Validate(null, o);
        result.Failed.Should().BeTrue();
        result.Failures!.Count().Should().BeGreaterThan(1);
    }
}
