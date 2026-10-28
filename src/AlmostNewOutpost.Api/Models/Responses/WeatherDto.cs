using AlmostNewOutpost.Api.Services;
using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// The current weather as the UI's banner needs it, including when the next check is due.
/// </summary>
public class WeatherDto
{
    public WeatherCondition Condition { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public bool IsDangerous { get; set; }

    public decimal Multiplier { get; set; }

    /// <summary>Null until the first successful fetch.</summary>
    public DateTime? FetchedAtUtc { get; set; }

    /// <summary>When the server will check the weather again. The UI counts down to this.</summary>
    public DateTime? NextRefreshUtc { get; set; }

    /// <summary>The server's clock, so the UI can correct for a wrong PC clock.</summary>
    public DateTime ServerTimeUtc { get; set; }

    /// <summary>"Random" (the stand-in provider), "Override" (set by an admin) or "None".</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>True when the last fetch failed and this is the last known condition.</summary>
    public bool IsStale { get; set; }

    public static WeatherDto FromSnapshot(WeatherSnapshot snapshot, DateTime serverTimeUtc)
    {
        return new WeatherDto
        {
            Condition = snapshot.Condition,
            DisplayName = GetDisplayName(snapshot.Condition),
            IsDangerous = PricingService.IsDangerous(snapshot.Condition),
            Multiplier = PricingService.GetMultiplier(snapshot.Condition),
            FetchedAtUtc = snapshot.FetchedAtUtc,
            NextRefreshUtc = snapshot.NextRefreshUtc,
            ServerTimeUtc = serverTimeUtc,
            Source = snapshot.Source,
            IsStale = snapshot.IsStale,
        };
    }

    public static string GetDisplayName(WeatherCondition condition)
    {
        switch (condition)
        {
            case WeatherCondition.Storm:
                return "Storm";
            case WeatherCondition.DustStorm:
                return "Dust Storm";
            case WeatherCondition.RadiationEvent:
                return "Radiation Event";
            default:
                return "Clear Conditions";
        }
    }
}
