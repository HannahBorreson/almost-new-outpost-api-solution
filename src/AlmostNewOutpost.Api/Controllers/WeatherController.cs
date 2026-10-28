using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Weather;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Api.Controllers;

/// <summary>
/// The current weather for the UI's banner. Read from the in-memory cache, never from the provider directly.
/// </summary>
[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly WeatherState _weatherState;
    private readonly TimeProvider _timeProvider;

    public WeatherController(WeatherState weatherState, TimeProvider timeProvider)
    {
        _weatherState = weatherState;
        _timeProvider = timeProvider;
    }

    [HttpGet("current")]
    [ProducesResponseType<WeatherDto>(StatusCodes.Status200OK)]
    public ActionResult<WeatherDto> GetCurrent()
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        return Ok(WeatherDto.FromSnapshot(_weatherState.GetSnapshot(), now));
    }
}
