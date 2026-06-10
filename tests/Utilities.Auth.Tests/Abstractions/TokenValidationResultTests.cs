using System.Security.Claims;
using FluentAssertions;
using Utilities.Auth;
using Xunit;

namespace Utilities.Auth.Tests.Abstractions;

public sealed class TokenValidationResultTests
{
    private static ClaimsPrincipal MakePrincipal() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1")], "test"));

    [Fact]
    public void Success_factory_sets_IsValid_and_Principal()
    {
        var principal = MakePrincipal();
        var result = TokenValidationResult.Success(principal);

        result.IsValid.Should().BeTrue();
        result.Principal.Should().BeSameAs(principal);
        result.FailureReason.Should().BeNull();
        result.FailureMessage.Should().BeNull();
    }

    [Fact]
    public void Success_factory_sets_ExpiresAt_when_provided()
    {
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var result = TokenValidationResult.Success(MakePrincipal(), expiry);

        result.ExpiresAt.Should().Be(expiry);
    }

    [Fact]
    public void Failure_factory_sets_IsValid_false_and_reason()
    {
        var result = TokenValidationResult.Failure(TokenValidationFailureReason.Expired, "Token has expired.");

        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(TokenValidationFailureReason.Expired);
        result.FailureMessage.Should().Be("Token has expired.");
        result.Principal.Should().BeNull();
    }

    [Fact]
    public void Failure_factory_works_without_message()
    {
        var result = TokenValidationResult.Failure(TokenValidationFailureReason.InvalidSignature);

        result.FailureMessage.Should().BeNull();
    }

    [Fact]
    public void IsExpired_false_when_ExpiresAt_not_set()
    {
        var result = TokenValidationResult.Success(MakePrincipal());

        result.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_false_when_ExpiresAt_in_future()
    {
        var result = TokenValidationResult.Success(MakePrincipal(), DateTimeOffset.UtcNow.AddHours(1));

        result.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_true_when_ExpiresAt_in_past()
    {
        var result = TokenValidationResult.Success(MakePrincipal(), DateTimeOffset.UtcNow.AddHours(-1));

        result.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void Record_equality_on_success_results()
    {
        var principal = MakePrincipal();
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var a = TokenValidationResult.Success(principal, expiry);
        var b = TokenValidationResult.Success(principal, expiry);

        a.Should().Be(b);
    }

    [Fact]
    public void Record_equality_on_failure_results()
    {
        var a = TokenValidationResult.Failure(TokenValidationFailureReason.Revoked, "msg");
        var b = TokenValidationResult.Failure(TokenValidationFailureReason.Revoked, "msg");

        a.Should().Be(b);
    }
}
