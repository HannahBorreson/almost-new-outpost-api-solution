using AlmostNewOutpost.Api.Options;
using Microsoft.Extensions.Options;

namespace AlmostNewOutpost.Api.Weather;

/// <summary>
/// A stand-in for a third-party weather API: picks a condition at random, weighted by the
/// Weather:Weights settings (Clear 55, Storm 15, DustStorm 15, RadiationEvent 15).
/// In the weather lab this is replaced by a provider that calls a real weather service.
/// Set Weather:RandomSeed to get the same sequence every time (useful in tests).
/// </summary>
public class RandomWeatherProvider : IWeatherProvider
{
    private readonly Random _random;
    private readonly WeatherOptions _options;
    private readonly object _lock = new object();

    public RandomWeatherProvider(IOptions<WeatherOptions> options)
    {
        _options = options.Value;
        _random = _options.RandomSeed.HasValue ? new Random(_options.RandomSeed.Value) : new Random();
    }

    public string SourceName => "Random";

    public Task<WeatherCondition> GetCurrentAsync(CancellationToken cancellationToken)
    {
        // A real provider would await an HTTP call here. We just roll the dice.
        WeatherCondition condition = PickCondition();
        return Task.FromResult(condition);
    }

    private WeatherCondition PickCondition()
    {
        WeatherWeights weights = _options.Weights;
        int total = weights.Clear + weights.Storm + weights.DustStorm + weights.RadiationEvent;

        int roll;
        lock (_lock)
        {
            // Random isn't thread-safe, so only one caller may use it at a time.
            roll = _random.Next(total);
        }

        if (roll < weights.Clear)
        {
            return WeatherCondition.Clear;
        }

        roll -= weights.Clear;
        if (roll < weights.Storm)
        {
            return WeatherCondition.Storm;
        }

        roll -= weights.Storm;
        if (roll < weights.DustStorm)
        {
            return WeatherCondition.DustStorm;
        }

        return WeatherCondition.RadiationEvent;
    }
}
