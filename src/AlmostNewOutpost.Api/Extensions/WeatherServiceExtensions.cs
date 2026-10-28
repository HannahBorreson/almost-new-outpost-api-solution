using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Api.Extensions;

/// <summary>
/// Registers the weather provider, the shared weather state and the background refresh.
/// </summary>
public static class WeatherServiceExtensions
{
    public static IServiceCollection AddOutpostWeather(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<WeatherState>();

        // The stand-in third-party provider. The weather lab swaps in a real one here.
        services.AddSingleton<IWeatherProvider, RandomWeatherProvider>();

        services.AddHostedService<WeatherRefreshService>();

        return services;
    }
}
