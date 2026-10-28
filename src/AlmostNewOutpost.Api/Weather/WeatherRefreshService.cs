using AlmostNewOutpost.Api.Options;
using Microsoft.Extensions.Options;

namespace AlmostNewOutpost.Api.Weather;

/// <summary>
/// A background service that asks the weather provider for the current condition once at
/// startup and then every Weather:RefreshMinutes, and stores the answer in WeatherState.
/// </summary>
public class WeatherRefreshService : BackgroundService
{
    private readonly IWeatherProvider _provider;
    private readonly WeatherState _state;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WeatherRefreshService> _logger;
    private readonly TimeSpan _interval;

    public WeatherRefreshService(
        IWeatherProvider provider,
        WeatherState state,
        IOptions<WeatherOptions> options,
        TimeProvider timeProvider,
        ILogger<WeatherRefreshService> logger)
    {
        _provider = provider;
        _state = state;
        _timeProvider = timeProvider;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(options.Value.RefreshMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshOnceAsync(stoppingToken);

        using PeriodicTimer timer = new PeriodicTimer(_interval, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The app is shutting down. Nothing to clean up.
        }
    }

    /// <summary>
    /// Fetches the weather once. Public so unit tests can call it directly.
    /// </summary>
    public async Task RefreshOnceAsync(CancellationToken cancellationToken)
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime nextRefresh = now.Add(_interval);

        try
        {
            WeatherCondition condition = await _provider.GetCurrentAsync(cancellationToken);
            _state.RecordSuccess(condition, _provider.SourceName, now, nextRefresh);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Keep the last known condition (marked stale) rather than failing requests.
            _logger.LogWarning(exception, "Weather refresh failed; keeping the last known condition.");
            _state.RecordFailure(nextRefresh);
        }
    }
}
