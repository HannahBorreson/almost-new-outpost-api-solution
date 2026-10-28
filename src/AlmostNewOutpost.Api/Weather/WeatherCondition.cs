namespace AlmostNewOutpost.Api.Weather;

/// <summary>
/// The four weather conditions the outpost knows about. Everything except Clear is dangerous
/// and raises prices (see PricingService).
/// </summary>
public enum WeatherCondition
{
    Clear,
    Storm,
    DustStorm,
    RadiationEvent,
}
