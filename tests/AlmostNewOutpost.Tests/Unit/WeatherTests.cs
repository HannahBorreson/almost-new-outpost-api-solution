using AlmostNewOutpost.Api.Options;
using AlmostNewOutpost.Api.Weather;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// The random provider, the shared weather state, and the refresh service.
/// </summary>
public class WeatherTests
{
    private static RandomWeatherProvider CreateProvider(int? seed)
    {
        return new RandomWeatherProvider(TestSettings.Wrap(new WeatherOptions { RandomSeed = seed }));
    }

    private static async Task<List<WeatherCondition>> DrawAsync(IWeatherProvider provider, int count)
    {
        List<WeatherCondition> draws = new List<WeatherCondition>();
        for (int i = 0; i < count; i++)
        {
            draws.Add(await provider.GetCurrentAsync(CancellationToken.None));
        }

        return draws;
    }

    [Fact]
    public async Task SeededRandomProvider_IsRepeatable()
    {
        List<WeatherCondition> first = await DrawAsync(CreateProvider(42), 200);
        List<WeatherCondition> second = await DrawAsync(CreateProvider(42), 200);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task RandomProvider_ProducesAllFourConditions_ClearMostOften()
    {
        List<WeatherCondition> draws = await DrawAsync(CreateProvider(7), 2000);

        foreach (WeatherCondition condition in Enum.GetValues<WeatherCondition>())
        {
            Assert.Contains(condition, draws);
        }

        int clearCount = draws.Count(c => c == WeatherCondition.Clear);
        Assert.InRange(clearCount, 900, 1300); // about 55% of 2000
    }

    [Fact]
    public void RandomProvider_ReportsItsSource()
    {
        Assert.Equal("Random", CreateProvider(1).SourceName);
    }

    [Fact]
    public void NewState_IsClear_AndNotStale()
    {
        WeatherSnapshot snapshot = new WeatherState().GetSnapshot();

        Assert.Equal(WeatherCondition.Clear, snapshot.Condition);
        Assert.False(snapshot.IsStale);
        Assert.Null(snapshot.FetchedAtUtc);
    }

    [Fact]
    public void Override_BeatsTheProvider_UntilCleared()
    {
        WeatherState state = new WeatherState();
        DateTime now = new DateTime(2026, 10, 27, 21, 0, 0, DateTimeKind.Utc);
        state.RecordSuccess(WeatherCondition.Storm, "Random", now, now.AddMinutes(5));

        state.SetOverride(WeatherCondition.Clear);
        WeatherSnapshot overridden = state.GetSnapshot();
        state.ClearOverride();
        WeatherSnapshot cleared = state.GetSnapshot();

        Assert.Equal(WeatherCondition.Clear, overridden.Condition);
        Assert.Equal("Override", overridden.Source);
        Assert.True(overridden.IsOverridden);
        Assert.Equal(WeatherCondition.Storm, cleared.Condition);
        Assert.Equal("Random", cleared.Source);
    }

    [Fact]
    public async Task RefreshService_StoresTheProvidersAnswer()
    {
        FixedTimeProvider clock = new FixedTimeProvider(new DateTimeOffset(2026, 10, 27, 21, 0, 0, TimeSpan.Zero));
        WeatherState state = new WeatherState();
        WeatherRefreshService service = CreateRefreshService(new SequenceWeatherProvider(WeatherCondition.DustStorm), state, clock);

        await service.RefreshOnceAsync(CancellationToken.None);
        WeatherSnapshot snapshot = state.GetSnapshot();

        Assert.Equal(WeatherCondition.DustStorm, snapshot.Condition);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, snapshot.FetchedAtUtc);
        Assert.Equal(clock.GetUtcNow().UtcDateTime.AddMinutes(5), snapshot.NextRefreshUtc);
        Assert.False(snapshot.IsStale);
    }

    [Fact]
    public async Task RefreshService_WhenProviderFails_KeepsLastConditionAndMarksStale()
    {
        FixedTimeProvider clock = new FixedTimeProvider(new DateTimeOffset(2026, 10, 27, 21, 0, 0, TimeSpan.Zero));
        WeatherState state = new WeatherState();
        SequenceWeatherProvider provider = new SequenceWeatherProvider(WeatherCondition.Storm);
        WeatherRefreshService service = CreateRefreshService(provider, state, clock);

        await service.RefreshOnceAsync(CancellationToken.None);
        provider.FailNextCall = true;
        clock.Advance(TimeSpan.FromMinutes(5));
        await service.RefreshOnceAsync(CancellationToken.None);
        WeatherSnapshot snapshot = state.GetSnapshot();

        Assert.Equal(WeatherCondition.Storm, snapshot.Condition);
        Assert.True(snapshot.IsStale);
        Assert.Equal(clock.GetUtcNow().UtcDateTime.AddMinutes(5), snapshot.NextRefreshUtc);
    }

    [Fact]
    public async Task RefreshService_WhenFirstFetchFails_StaysClear()
    {
        FixedTimeProvider clock = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
        WeatherState state = new WeatherState();
        SequenceWeatherProvider provider = new SequenceWeatherProvider(WeatherCondition.RadiationEvent) { FailNextCall = true };

        await CreateRefreshService(provider, state, clock).RefreshOnceAsync(CancellationToken.None);

        Assert.Equal(WeatherCondition.Clear, state.GetSnapshot().Condition);
        Assert.True(state.GetSnapshot().IsStale);
    }

    private static WeatherRefreshService CreateRefreshService(IWeatherProvider provider, WeatherState state, TimeProvider clock)
    {
        return new WeatherRefreshService(
            provider,
            state,
            TestSettings.Wrap(new WeatherOptions { RefreshMinutes = 5 }),
            clock,
            NullLogger<WeatherRefreshService>.Instance);
    }
}
