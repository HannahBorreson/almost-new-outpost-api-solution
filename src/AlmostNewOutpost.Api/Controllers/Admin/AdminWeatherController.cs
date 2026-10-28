using AlmostNewOutpost.Api.Models.Constants;
using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Weather;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Api.Controllers.Admin;

/// <summary>
/// Force a weather condition for demos and tests, or go back to the provider's weather.
/// MerchantAdmin only.
/// </summary>
[ApiController]
[Route("api/admin/weather/override")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminWeatherController : ControllerBase
{
    private readonly WeatherState _weatherState;
    private readonly TimeProvider _timeProvider;

    public AdminWeatherController(WeatherState weatherState, TimeProvider timeProvider)
    {
        _weatherState = weatherState;
        _timeProvider = timeProvider;
    }

    /// <summary>Force a condition, for example { "condition": "DustStorm" }. It stays until cleared.</summary>
    [HttpPut]
    [ProducesResponseType<WeatherDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<WeatherDto> SetOverride(WeatherOverrideRequest request)
    {
        // [Required] has already rejected a missing condition, so Value is safe here.
        _weatherState.SetOverride(request.Condition!.Value);
        return Ok(WeatherDto.FromSnapshot(_weatherState.GetSnapshot(), _timeProvider.GetUtcNow().UtcDateTime));
    }

    /// <summary>Clear the override and return to the provider's weather.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ClearOverride()
    {
        _weatherState.ClearOverride();
        return NoContent();
    }
}
