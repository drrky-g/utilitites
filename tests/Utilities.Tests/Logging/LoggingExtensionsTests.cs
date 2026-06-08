using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Utilities.Logging;
using Utilities.Tests.Helpers;

namespace Utilities.Tests.Logging;

public class LoggingExtensionsTests
{
    private readonly CapturingLogger _logger = new();

    // -------------------------------------------------------------------------
    // OperationCompleted
    // -------------------------------------------------------------------------

    [Fact]
    public void OperationCompleted_LogsAtInformationLevel()
    {
        _logger.OperationCompleted("TestOp", 42.5);

        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void OperationCompleted_MessageContainsOperationNameAndElapsed()
    {
        _logger.OperationCompleted("ProcessOrder", 123.456);

        var message = _logger.Entries.Single().Message;
        message.Should().Contain("ProcessOrder");
        message.Should().Contain("123.46"); // rounded to 2dp by the format string
    }

    [Fact]
    public void OperationCompleted_UsesCorrectEventId()
    {
        _logger.OperationCompleted("TestOp", 1.0);

        _logger.EntryFor(1000).Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // OperationFailed
    // -------------------------------------------------------------------------

    [Fact]
    public void OperationFailed_LogsAtErrorLevel()
    {
        _logger.OperationFailed("TestOp", 99.9);

        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void OperationFailed_AttachesExceptionWhenProvided()
    {
        var ex = new InvalidOperationException("boom");

        _logger.OperationFailed("TestOp", 10.0, ex);

        _logger.Entries.Single().Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void OperationFailed_ExceptionIsNullWhenNotProvided()
    {
        _logger.OperationFailed("TestOp", 10.0);

        _logger.Entries.Single().Exception.Should().BeNull();
    }

    [Fact]
    public void OperationFailed_UsesCorrectEventId()
    {
        _logger.OperationFailed("TestOp", 1.0);

        _logger.EntryFor(1001).Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // DependencyCallStarted
    // -------------------------------------------------------------------------

    [Fact]
    public void DependencyCallStarted_LogsAtDebugLevel()
    {
        _logger.DependencyCallStarted("PaymentGateway", "https://pay.example.com");

        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void DependencyCallStarted_MessageContainsDependencyNameAndAddress()
    {
        _logger.DependencyCallStarted("PaymentGateway", "https://pay.example.com");

        var message = _logger.Entries.Single().Message;
        message.Should().Contain("PaymentGateway");
        message.Should().Contain("https://pay.example.com");
    }

    [Fact]
    public void DependencyCallStarted_UsesCorrectEventId()
    {
        _logger.DependencyCallStarted("SomeDep", "addr");

        _logger.EntryFor(2000).Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // DependencyCallCompleted
    // -------------------------------------------------------------------------

    [Fact]
    public void DependencyCallCompleted_LogsAtDebugLevel()
    {
        _logger.DependencyCallCompleted("PaymentGateway", 200, 55.5);

        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void DependencyCallCompleted_MessageContainsStatusCodeAndElapsed()
    {
        _logger.DependencyCallCompleted("PaymentGateway", 200, 55.5);

        var message = _logger.Entries.Single().Message;
        message.Should().Contain("200");
        message.Should().Contain("55.50");
    }

    [Fact]
    public void DependencyCallCompleted_UsesCorrectEventId()
    {
        _logger.DependencyCallCompleted("SomeDep", 200, 1.0);

        _logger.EntryFor(2001).Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // DependencyCallFailed
    // -------------------------------------------------------------------------

    [Fact]
    public void DependencyCallFailed_LogsAtErrorLevel()
    {
        _logger.DependencyCallFailed("PaymentGateway", 300.0);

        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void DependencyCallFailed_AttachesExceptionWhenProvided()
    {
        var ex = new HttpRequestException("timeout");

        _logger.DependencyCallFailed("PaymentGateway", 300.0, ex);

        _logger.Entries.Single().Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void DependencyCallFailed_UsesCorrectEventId()
    {
        _logger.DependencyCallFailed("SomeDep", 1.0);

        _logger.EntryFor(2002).Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // ValidationFailed
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidationFailed_LogsAtWarningLevel()
    {
        _logger.ValidationFailed("EmailAddress", "must not be empty");

        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void ValidationFailed_MessageContainsFieldNameAndReason()
    {
        _logger.ValidationFailed("EmailAddress", "must not be empty");

        var message = _logger.Entries.Single().Message;
        message.Should().Contain("EmailAddress");
        message.Should().Contain("must not be empty");
    }

    [Fact]
    public void ValidationFailed_UsesCorrectEventId()
    {
        _logger.ValidationFailed("Field", "reason");

        _logger.EntryFor(3000).Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // BeginOperationScope
    // -------------------------------------------------------------------------

    [Fact]
    public void BeginOperationScope_ReturnsNonNullDisposable()
    {
        using var scope = _logger.BeginOperationScope("TestOp", "corr-123");

        scope.Should().NotBeNull();
    }

    [Fact]
    public void BeginOperationScope_WorksWithNullCorrelationId()
    {
        var act = () =>
        {
            using var scope = _logger.BeginOperationScope("TestOp", correlationId: null);
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void BeginOperationScope_ScopeDisposeDoesNotThrow()
    {
        var scope = _logger.BeginOperationScope("TestOp", "corr-abc");

        var act = () => scope?.Dispose();

        act.Should().NotThrow();
    }
}
