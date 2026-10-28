using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Replaces the random provider in API tests: the weather is always Clear, so prices are
/// predictable. Tests that need a storm use the admin override.
/// </summary>
public class FixedWeatherProvider : IWeatherProvider
{
    public string SourceName => "Fixed";

    public Task<WeatherCondition> GetCurrentAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(WeatherCondition.Clear);
    }
}
