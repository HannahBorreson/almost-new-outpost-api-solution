using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// A test weather provider that always answers with one condition, or throws when told to,
/// so tests can check what happens when the weather service is down.
/// </summary>
public class SequenceWeatherProvider : IWeatherProvider
{
    private readonly WeatherCondition _condition;

    public SequenceWeatherProvider(WeatherCondition condition)
    {
        _condition = condition;
    }

    public bool FailNextCall { get; set; }

    public string SourceName => "Test";

    public Task<WeatherCondition> GetCurrentAsync(CancellationToken cancellationToken)
    {
        if (FailNextCall)
        {
            FailNextCall = false;
            throw new HttpRequestException("The weather service is down.");
        }

        return Task.FromResult(_condition);
    }
}
