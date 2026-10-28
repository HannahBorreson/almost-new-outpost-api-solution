namespace AlmostNewOutpost.Api.Weather;

/// <summary>
/// Somewhere we can ask "what's the weather right now?". Async on purpose: a real
/// provider will make an HTTP call to a third-party weather service.
/// </summary>
public interface IWeatherProvider
{
    /// <summary>Short name shown in WeatherDto.source, for example "Random".</summary>
    string SourceName { get; }

    Task<WeatherCondition> GetCurrentAsync(CancellationToken cancellationToken);
}
