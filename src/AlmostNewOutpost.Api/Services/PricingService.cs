using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// The weather pricing rule: dangerous weather (Storm, DustStorm, RadiationEvent) raises every
/// price by 25%; Clear weather leaves prices alone. Pure: no database, no HTTP, no clock.
/// </summary>
public static class PricingService
{
    public const decimal DangerMultiplier = 1.25m;

    public static bool IsDangerous(WeatherCondition condition)
    {
        return condition != WeatherCondition.Clear;
    }

    public static decimal GetMultiplier(WeatherCondition condition)
    {
        return IsDangerous(condition) ? DangerMultiplier : 1m;
    }

    public static int GetCurrentPrice(int basePrice, WeatherCondition condition)
    {
        if (!IsDangerous(condition))
        {
            return basePrice;
        }

        // decimal (not double) so 10 * 1.25 is exactly 12.5, which then rounds up to 13.
        decimal raised = basePrice * DangerMultiplier;
        return (int)Math.Round(raised, MidpointRounding.AwayFromZero);
    }
}
