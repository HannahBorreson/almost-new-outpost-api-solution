using System.ComponentModel.DataAnnotations;
using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Api.Models.Requests;

/// <summary>
/// Body of PUT /api/admin/weather/override, for example { "condition": "DustStorm" }.
/// </summary>
public class WeatherOverrideRequest
{
    // An unknown name like "Tornado" fails JSON conversion and comes back as a 400.
    // EnumDataType also rejects numbers that aren't a defined condition, such as 99.
    [Required]
    [EnumDataType(typeof(WeatherCondition), ErrorMessage = "condition must be Clear, Storm, DustStorm or RadiationEvent.")]
    public WeatherCondition? Condition { get; set; }
}
