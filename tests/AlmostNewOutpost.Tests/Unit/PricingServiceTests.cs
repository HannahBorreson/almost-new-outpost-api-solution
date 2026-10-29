using AlmostNewOutpost.Api.Services;
using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// The weather pricing rule: dangerous weather adds 25%, rounded half away from zero.
/// </summary>
public class PricingServiceTests
{
    [Theory]
    [InlineData(12, 15)]
    [InlineData(100, 125)]
    [InlineData(8, 10)]
    [InlineData(10, 13)] // 12.5 rounds up
    [InlineData(2, 3)]   // 2.5 rounds up
    public void DustStorm_RaisesPriceBy25Percent(int basePrice, int expected)
    {
        // Act
        int price = PricingService.GetCurrentPrice(basePrice, WeatherCondition.DustStorm);

        // Assert
        Assert.Equal(expected, price);
    }

    [Theory]
    [InlineData(WeatherCondition.Storm)]
    [InlineData(WeatherCondition.DustStorm)]
    [InlineData(WeatherCondition.RadiationEvent)]
    public void EveryDangerousCondition_MultipliesThePrice(WeatherCondition condition)
    {
        // Act
        bool isDangerous = PricingService.IsDangerous(condition);
        int price = PricingService.GetCurrentPrice(100, condition);
        decimal multiplier = PricingService.GetMultiplier(condition);

        // Assert
        Assert.True(isDangerous);
        Assert.Equal(125, price);
        Assert.Equal(1.25m, multiplier);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(100)]
    [InlineData(100000)]
    public void Clear_LeavesThePriceUnchanged(int basePrice)
    {
        // Act
        bool isDangerous = PricingService.IsDangerous(WeatherCondition.Clear);
        int price = PricingService.GetCurrentPrice(basePrice, WeatherCondition.Clear);
        decimal multiplier = PricingService.GetMultiplier(WeatherCondition.Clear);

        // Assert
        Assert.False(isDangerous);
        Assert.Equal(basePrice, price);
        Assert.Equal(1m, multiplier);
    }
}
