using System.Net;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Tests.Api.Infrastructure;

namespace AlmostNewOutpost.Tests.Api.Acceptance;

/// <summary>
/// Story: when a dust storm hits, every price rises by 25%, and purchases are charged the higher price.
/// </summary>
[Collection(ApiCollection.Name)]
public class DustStormRaisesPricesTests : ApiTestBase
{
    public DustStormRaisesPricesTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiFact]
    public async Task DustStorm_RaisesThePriceAMedicPays()
    {
        // Given a Medic, and a Medical Supply Kit that costs 100 credits on a clear day
        HttpClient medic = await ClientForAsync("medic");
        HttpClient admin = await ClientForAsync("admin");
        ProductDto clearDay = await ReadAsync<ProductDto>(await medic.GetAsync("/api/products/12"));
        Assert.Equal(100, clearDay.CurrentPrice);

        // When a dust storm arrives
        HttpResponseMessage storm = await admin.PutAsJsonAsync("/api/admin/weather/override", new { condition = "DustStorm" });
        Assert.Equal(HttpStatusCode.OK, storm.StatusCode);

        // Then the kit costs 125 credits, and the base price is unchanged
        ProductDto stormyDay = await ReadAsync<ProductDto>(await medic.GetAsync("/api/products/12"));
        Assert.Equal(100, stormyDay.BasePrice);
        Assert.Equal(125, stormyDay.CurrentPrice);

        // And buying it now charges 125 credits
        PurchaseDto purchase = await ReadAsync<PurchaseDto>(await medic.PostAsJsonAsync("/api/purchases", new { productId = 12 }));
        Assert.Equal(125, purchase.FinalPrice);

        // When the storm passes, prices return to normal
        await admin.DeleteAsync("/api/admin/weather/override");
        ProductDto afterStorm = await ReadAsync<ProductDto>(await medic.GetAsync("/api/products/12"));
        Assert.Equal(100, afterStorm.CurrentPrice);
    }
}
