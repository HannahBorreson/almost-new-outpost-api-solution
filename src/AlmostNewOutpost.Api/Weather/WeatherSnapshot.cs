namespace AlmostNewOutpost.Api.Weather;

/// <summary>
/// A read-only copy of the weather at one moment, handed out by WeatherState so callers
/// never see a half-updated value.
/// </summary>
public class WeatherSnapshot
{
    public WeatherCondition Condition { get; init; } = WeatherCondition.Clear;

    public string Source { get; init; } = "None";

    public DateTime? FetchedAtUtc { get; init; }

    public DateTime? NextRefreshUtc { get; init; }

    public bool IsStale { get; init; }

    public bool IsOverridden { get; init; }
}
