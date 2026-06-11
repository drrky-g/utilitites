namespace Utilities.Auth.Tests;

internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan offset) => _utcNow = _utcNow.Add(offset);

    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
}
