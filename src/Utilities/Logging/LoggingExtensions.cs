using Microsoft.Extensions.Logging;

namespace Utilities.Logging;

/// <summary>
/// Extension methods for <see cref="ILogger"/> that provide structured, high-performance
/// logging helpers using <c>LoggerMessage.Define</c> source-generated delegates.
///
/// Why source-generated delegates over direct logger.Log() calls?
///   - Parsing of the message template happens once at startup, not per call.
///   - No boxing of value-type parameters.
///   - Optimal for hot paths (e.g. per-request, per-item loops).
/// </summary>
public static class LoggingExtensions
{
    // -------------------------------------------------------------------------
    // Operation timing
    // -------------------------------------------------------------------------

    private static readonly Action<ILogger, string, double, Exception?> _operationCompleted =
        LoggerMessage.Define<string, double>(
            LogLevel.Information,
            new EventId(1000, nameof(OperationCompleted)),
            "Operation '{OperationName}' completed in {ElapsedMs:F2} ms");

    private static readonly Action<ILogger, string, double, Exception?> _operationFailed =
        LoggerMessage.Define<string, double>(
            LogLevel.Error,
            new EventId(1001, nameof(OperationFailed)),
            "Operation '{OperationName}' failed after {ElapsedMs:F2} ms");

    /// <summary>Logs a successful operation with its elapsed time in milliseconds.</summary>
    public static void OperationCompleted(this ILogger logger, string operationName, double elapsedMs)
        => _operationCompleted(logger, operationName, elapsedMs, null);

    /// <summary>Logs a failed operation with its elapsed time and the originating exception.</summary>
    public static void OperationFailed(this ILogger logger, string operationName, double elapsedMs, Exception? exception = null)
        => _operationFailed(logger, operationName, elapsedMs, exception);

    // -------------------------------------------------------------------------
    // Dependency / external call tracing
    // -------------------------------------------------------------------------

    private static readonly Action<ILogger, string, string, Exception?> _dependencyCallStarted =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(2000, nameof(DependencyCallStarted)),
            "Calling dependency '{DependencyName}' → '{TargetAddress}'");

    private static readonly Action<ILogger, string, int, double, Exception?> _dependencyCallCompleted =
        LoggerMessage.Define<string, int, double>(
            LogLevel.Debug,
            new EventId(2001, nameof(DependencyCallCompleted)),
            "Dependency '{DependencyName}' responded with status {StatusCode} in {ElapsedMs:F2} ms");

    private static readonly Action<ILogger, string, double, Exception?> _dependencyCallFailed =
        LoggerMessage.Define<string, double>(
            LogLevel.Error,
            new EventId(2002, nameof(DependencyCallFailed)),
            "Dependency '{DependencyName}' failed after {ElapsedMs:F2} ms");

    /// <summary>Logs the start of an outbound call to a dependency (HTTP, database, queue, etc.).</summary>
    public static void DependencyCallStarted(this ILogger logger, string dependencyName, string targetAddress)
        => _dependencyCallStarted(logger, dependencyName, targetAddress, null);

    /// <summary>Logs a completed outbound dependency call with its HTTP/status code and elapsed time.</summary>
    public static void DependencyCallCompleted(this ILogger logger, string dependencyName, int statusCode, double elapsedMs)
        => _dependencyCallCompleted(logger, dependencyName, statusCode, elapsedMs, null);

    /// <summary>Logs a failed outbound dependency call.</summary>
    public static void DependencyCallFailed(this ILogger logger, string dependencyName, double elapsedMs, Exception? exception = null)
        => _dependencyCallFailed(logger, dependencyName, elapsedMs, exception);

    // -------------------------------------------------------------------------
    // Validation / guard helpers
    // -------------------------------------------------------------------------

    private static readonly Action<ILogger, string, string, Exception?> _validationFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(3000, nameof(ValidationFailed)),
            "Validation failed for '{FieldName}': {Reason}");

    /// <summary>Logs a validation failure with the field name and a human-readable reason.</summary>
    public static void ValidationFailed(this ILogger logger, string fieldName, string reason)
        => _validationFailed(logger, fieldName, reason, null);

    // -------------------------------------------------------------------------
    // Scope helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Begins a named logging scope that attaches <paramref name="operationName"/> and
    /// <paramref name="correlationId"/> as structured properties to every log entry
    /// emitted within the returned <see cref="IDisposable"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// using (logger.BeginOperationScope("ProcessOrder", correlationId))
    /// {
    ///     // all log calls here carry OperationName + CorrelationId
    /// }
    /// </code>
    /// </example>
    public static IDisposable? BeginOperationScope(this ILogger logger, string operationName, string? correlationId = null)
        => logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationName"]  = operationName,
            ["CorrelationId"]  = correlationId
        });
}
