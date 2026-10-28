using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Options;

/// <summary>
/// Weather settings, bound from the "Weather" section of appsettings.json.
/// </summary>
public class WeatherOptions
{
    public const string SectionName = "Weather";

    /// <summary>Which provider to use. Only "Random" exists until the weather lab.</summary>
    [Required]
    public string Provider { get; set; } = "Random";

    [Range(1, 1440)]
    public int RefreshMinutes { get; set; } = 5;

    /// <summary>Optional. The same seed always gives the same sequence of conditions.</summary>
    public int? RandomSeed { get; set; }

    public WeatherWeights Weights { get; set; } = new WeatherWeights();
}
