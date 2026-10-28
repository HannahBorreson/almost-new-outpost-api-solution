namespace AlmostNewOutpost.Api.Weather;

/// <summary>
/// The one shared, in-memory copy of the current weather (registered as a singleton).
/// The background refresh writes to it; every price calculation reads from it, so a request
/// never waits on the weather service. An admin override wins until it is cleared.
/// </summary>
public class WeatherState
{
    // Many requests read this while the background service writes to it,
    // so every read and write happens inside this lock.
    private readonly object _lock = new object();

    // Until the first successful fetch the weather is Clear, so the shop never
    // overcharges because of missing data.
    private WeatherCondition _condition = WeatherCondition.Clear;
    private string _source = "None";
    private DateTime? _fetchedAtUtc;
    private DateTime? _nextRefreshUtc;
    private bool _isStale;
    private WeatherCondition? _override;

    public WeatherSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            bool isOverridden = _override.HasValue;
            return new WeatherSnapshot
            {
                Condition = _override ?? _condition,
                Source = isOverridden ? "Override" : _source,
                FetchedAtUtc = _fetchedAtUtc,
                NextRefreshUtc = _nextRefreshUtc,
                IsStale = _isStale,
                IsOverridden = isOverridden,
            };
        }
    }

    /// <summary>The provider answered: store the new condition.</summary>
    public void RecordSuccess(WeatherCondition condition, string source, DateTime fetchedAtUtc, DateTime nextRefreshUtc)
    {
        lock (_lock)
        {
            _condition = condition;
            _source = source;
            _fetchedAtUtc = fetchedAtUtc;
            _nextRefreshUtc = nextRefreshUtc;
            _isStale = false;
        }
    }

    /// <summary>The provider failed: keep the last condition but mark it as stale.</summary>
    public void RecordFailure(DateTime nextRefreshUtc)
    {
        lock (_lock)
        {
            _nextRefreshUtc = nextRefreshUtc;
            _isStale = true;
        }
    }

    public void SetOverride(WeatherCondition condition)
    {
        lock (_lock)
        {
            _override = condition;
        }
    }

    public void ClearOverride()
    {
        lock (_lock)
        {
            _override = null;
        }
    }
}
