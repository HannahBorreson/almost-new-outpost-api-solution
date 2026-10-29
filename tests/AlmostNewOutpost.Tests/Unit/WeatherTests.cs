using AlmostNewOutpost.Api.Options;
using AlmostNewOutpost.Api.Weather;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
        // Arrange
        RandomWeatherProvider firstProvider = CreateProvider(42);
        RandomWeatherProvider secondProvider = CreateProvider(42);

        // Act
        List<WeatherCondition> first = await DrawAsync(firstProvider, 200);
        List<WeatherCondition> second = await DrawAsync(secondProvider, 200);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task RandomProvider_ProducesAllFourConditions_ClearMostOften()
    {
        // Arrange
        RandomWeatherProvider provider = CreateProvider(7);

        // Act
        List<WeatherCondition> draws = await DrawAsync(provider, 2000);

        // Assert
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
        // Arrange
        RandomWeatherProvider provider = CreateProvider(1);

        // Act
        string source = provider.SourceName;

        // Assert
        Assert.Equal("Random", source);
    }

    [Fact]
    public void NewState_IsClear_AndNotStale()
    {
        // Arrange
        WeatherState state = new WeatherState();

        // Act
        WeatherSnapshot snapshot = state.GetSnapshot();

        // Assert
        Assert.Equal(WeatherCondition.Clear, snapshot.Condition);
        Assert.False(snapshot.IsStale);
        Assert.Null(snapshot.FetchedAtUtc);
    }

    [Fact]
    public void Override_BeatsTheProvider_UntilCleared()
    {
        // Arrange
        WeatherState state = new WeatherState();
        DateTime now = new DateTime(2026, 10, 27, 21, 0, 0, DateTimeKind.Utc);
        state.RecordSuccess(WeatherCondition.Storm, "Random", now, now.AddMinutes(5));

        // Act
        state.SetOverride(WeatherCondition.Clear);
        WeatherSnapshot overridden = state.GetSnapshot();
        state.ClearOverride();
        WeatherSnapshot cleared = state.GetSnapshot();

        // Assert
        Assert.Equal(WeatherCondition.Clear, overridden.Condition);
        Assert.Equal("Override", overridden.Source);
        Assert.True(overridden.IsOverridden);
        Assert.Equal(WeatherCondition.Storm, cleared.Condition);
        Assert.Equal("Random", cleared.Source);
    }

    [Fact]
    public async Task RefreshService_StoresTheProvidersAnswer()
    {
        // Arrange
        FakeTimeProvider clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 27, 21, 0, 0, TimeSpan.Zero));
        WeatherState state = new WeatherState();
        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.SourceName.Returns("Test");
        provider.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(WeatherCondition.DustStorm);
        WeatherRefreshService service = CreateRefreshService(provider, state, clock);

        // Act
        await service.RefreshOnceAsync(CancellationToken.None);
        WeatherSnapshot snapshot = state.GetSnapshot();

        // Assert
        Assert.Equal(WeatherCondition.DustStorm, snapshot.Condition);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, snapshot.FetchedAtUtc);
        Assert.Equal(clock.GetUtcNow().UtcDateTime.AddMinutes(5), snapshot.NextRefreshUtc);
        Assert.False(snapshot.IsStale);
    }

    [Fact]
    public async Task RefreshService_WhenProviderFails_KeepsLastConditionAndMarksStale()
    {
        // Arrange
        FakeTimeProvider clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 27, 21, 0, 0, TimeSpan.Zero));
        WeatherState state = new WeatherState();
        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.SourceName.Returns("Test");
        // The first call answers Storm. The second call fails because the weather service is down.
        provider.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(
            callInfo => Task.FromResult(WeatherCondition.Storm),
            callInfo => throw new HttpRequestException("The weather service is down."));
        WeatherRefreshService service = CreateRefreshService(provider, state, clock);
        await service.RefreshOnceAsync(CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(5));

        // Act
        await service.RefreshOnceAsync(CancellationToken.None);
        WeatherSnapshot snapshot = state.GetSnapshot();

        // Assert
        Assert.Equal(WeatherCondition.Storm, snapshot.Condition);
        Assert.True(snapshot.IsStale);
        Assert.Equal(clock.GetUtcNow().UtcDateTime.AddMinutes(5), snapshot.NextRefreshUtc);
    }

    [Fact]
    public async Task RefreshService_WhenFirstFetchFails_StaysClear()
    {
        // Arrange
        FakeTimeProvider clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        WeatherState state = new WeatherState();
        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.SourceName.Returns("Test");
        provider.GetCurrentAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("The weather service is down."));
        WeatherRefreshService service = CreateRefreshService(provider, state, clock);

        // Act
        await service.RefreshOnceAsync(CancellationToken.None);
        WeatherSnapshot snapshot = state.GetSnapshot();

        // Assert
        Assert.Equal(WeatherCondition.Clear, snapshot.Condition);
        Assert.True(snapshot.IsStale);
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
