using System.Net;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Weather;
using AlmostNewOutpost.Tests.Api.Infrastructure;

namespace AlmostNewOutpost.Tests.Api;

/// <summary>
/// GET /api/weather/current and the admin weather override.
/// </summary>
[Collection(ApiCollection.Name)]
public class WeatherEndpointsTests : ApiTestBase
{
    public WeatherEndpointsTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiFact]
    public async Task Current_ReturnsTheWeatherShape()
    {
        HttpClient client = await ClientForAsync("citizen");

        HttpResponseMessage response = await client.GetAsync("/api/weather/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        WeatherDto weather = await ReadAsync<WeatherDto>(response);
        Assert.Equal(WeatherCondition.Clear, weather.Condition);
        Assert.Equal("Clear Conditions", weather.DisplayName);
        Assert.False(weather.IsDangerous);
        Assert.Equal(1m, weather.Multiplier);
        Assert.Equal("Fixed", weather.Source);
        Assert.NotNull(weather.NextRefreshUtc);
        Assert.InRange(weather.ServerTimeUtc, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [ApiFact]
    public async Task Current_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await AnonymousClient().GetAsync("/api/weather/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [ApiFact]
    public async Task AdminOverride_DustStorm_RaisesEveryPrice_AndDeleteRevertsIt()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage set = await admin.PutAsJsonAsync("/api/admin/weather/override", new { condition = "DustStorm" });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        WeatherDto weather = await ReadAsync<WeatherDto>(set);
        Assert.Equal(WeatherCondition.DustStorm, weather.Condition);
        Assert.Equal("Dust Storm", weather.DisplayName);
        Assert.True(weather.IsDangerous);
        Assert.Equal(1.25m, weather.Multiplier);
        Assert.Equal("Override", weather.Source);

        List<ProductDto> stormy = await ReadAsync<List<ProductDto>>(await admin.GetAsync("/api/products"));
        Assert.All(stormy, p => Assert.Equal((int)Math.Round(p.BasePrice * 1.25m, MidpointRounding.AwayFromZero), p.CurrentPrice));

        HttpResponseMessage cleared = await admin.DeleteAsync("/api/admin/weather/override");
        Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);

        List<ProductDto> calm = await ReadAsync<List<ProductDto>>(await admin.GetAsync("/api/products"));
        Assert.All(calm, p => Assert.Equal(p.BasePrice, p.CurrentPrice));
    }

    [ApiFact]
    public async Task Override_UnknownCondition_Returns400()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.PutAsJsonAsync("/api/admin/weather/override", new { condition = "Tornado" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [ApiFact]
    public async Task Override_AsMedic_Returns403()
    {
        HttpClient medic = await ClientForAsync("medic");

        HttpResponseMessage set = await medic.PutAsJsonAsync("/api/admin/weather/override", new { condition = "Storm" });
        HttpResponseMessage clear = await medic.DeleteAsync("/api/admin/weather/override");

        Assert.Equal(HttpStatusCode.Forbidden, set.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, clear.StatusCode);
    }
}
