namespace Tests;

/// <summary>
/// Fake TimeProvider for testing the 48-hour reopen window.
/// Allows setting a specific time for deterministic tests.
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset? utcNow = null)
    {
        _utcNow = utcNow ?? DateTimeOffset.UtcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;

    public void Advance(TimeSpan timeSpan) => _utcNow = _utcNow.Add(timeSpan);
}
