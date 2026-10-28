namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// A clock that always says the same time, so tests that involve time give the same answer every run.
/// </summary>
public class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _now;
    }

    public void Advance(TimeSpan amount)
    {
        _now = _now.Add(amount);
    }
}
