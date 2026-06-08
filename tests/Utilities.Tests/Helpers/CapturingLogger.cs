using Microsoft.Extensions.Logging;

namespace Utilities.Tests.Helpers;

/// <summary>
/// A test-only <see cref="ILogger"/> that captures every log entry so tests can
/// assert on what was logged without taking a dependency on a real logging backend.
/// </summary>
public sealed class CapturingLogger : ILogger
{
    private readonly List<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }

    /// <summary>Returns the first entry matching <paramref name="eventId"/>, or null.</summary>
    public LogEntry? EntryFor(int eventId)
        => _entries.FirstOrDefault(e => e.EventId.Id == eventId);

    /// <summary>Clears all captured entries.</summary>
    public void Clear() => _entries.Clear();

    public record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
