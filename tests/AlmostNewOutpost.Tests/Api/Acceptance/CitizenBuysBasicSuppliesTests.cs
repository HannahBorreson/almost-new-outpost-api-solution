using System.Net;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Tests.Api.Infrastructure;

namespace AlmostNewOutpost.Tests.Api.Acceptance;

/// <summary>
/// Story: as a Citizen, I can buy basic supplies, and they show up in my history.
/// </summary>
[Collection(ApiCollection.Name)]
public class CitizenBuysBasicSuppliesTests : ApiTestBase
{
    public CitizenBuysBasicSuppliesTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiFact]
    public async Task Citizen_BuysCleanWater_AndSeesItInHistory()
    {
        // Given a signed-in Citizen on a clear day
        HttpClient citizen = await ClientForAsync("citizen");

        // When they look at Clean Water
        ProductDto water = await ReadAsync<ProductDto>(await citizen.GetAsync("/api/products/1"));

        // Then it is available to them at its base price
        Assert.True(water.CanPurchase);
        Assert.Equal(12, water.CurrentPrice);

        // When they buy it
        HttpResponseMessage response = await citizen.PostAsJsonAsync("/api/purchases", new { productId = 1 });

        // Then the purchase succeeds at 12 credits
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PurchaseDto purchase = await ReadAsync<PurchaseDto>(response);
        Assert.Equal(12, purchase.FinalPrice);

        // And it is the newest entry in their purchase history
        List<PurchaseDto> history = await ReadAsync<List<PurchaseDto>>(await citizen.GetAsync("/api/purchases/me?limit=5"));
        Assert.Equal(purchase.PurchaseId, history[0].PurchaseId);
    }
}
