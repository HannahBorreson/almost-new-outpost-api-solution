using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Options;

/// <summary>
/// How likely RandomWeatherProvider is to pick each condition. The numbers are relative
/// weights (they don't have to add up to 100).
/// </summary>
public class WeatherWeights
{
    [Range(0, 1000)]
    public int Clear { get; set; } = 55;

    [Range(0, 1000)]
    public int Storm { get; set; } = 15;

    [Range(0, 1000)]
    public int DustStorm { get; set; } = 15;

    [Range(0, 1000)]
    public int RadiationEvent { get; set; } = 15;

    public int Total => Clear + Storm + DustStorm + RadiationEvent;
}
