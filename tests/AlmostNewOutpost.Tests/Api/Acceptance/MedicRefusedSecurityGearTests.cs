using System.Net;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Tests.Api.Infrastructure;

namespace AlmostNewOutpost.Tests.Api.Acceptance;

/// <summary>
/// Story: as a Medic, I can see Security gear, but I am not allowed to buy it.
/// </summary>
[Collection(ApiCollection.Name)]
public class MedicRefusedSecurityGearTests : ApiTestBase
{
    public MedicRefusedSecurityGearTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiFact]
    public async Task Medic_CannotBuyTheStunBaton()
    {
        // Given a signed-in Medic
        HttpClient medic = await ClientForAsync("medic");
        List<PurchaseDto> before = await ReadAsync<List<PurchaseDto>>(await medic.GetAsync("/api/purchases/me"));

        // When they look at the Stun Baton
        ProductDto baton = await ReadAsync<ProductDto>(await medic.GetAsync("/api/products/15"));

        // Then they can see it, but it is marked as not purchasable
        Assert.Equal("Security", baton.CategoryName);
        Assert.False(baton.CanPurchase);

        // When they try to buy it anyway
        HttpResponseMessage response = await medic.PostAsJsonAsync("/api/purchases", new { productId = 15 });

        // Then the server refuses
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // And nothing was added to their history
        List<PurchaseDto> after = await ReadAsync<List<PurchaseDto>>(await medic.GetAsync("/api/purchases/me"));
        Assert.Equal(before.Count, after.Count);
    }
}
