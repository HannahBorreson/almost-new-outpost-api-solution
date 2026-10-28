using System.Net;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Tests.Api.Infrastructure;

namespace AlmostNewOutpost.Tests.Api.Acceptance;

/// <summary>
/// Story: as the merchant, I can add a new product to the shelves and later remove it.
/// </summary>
[Collection(ApiCollection.Name)]
public class AdminAddsAndRemovesProductTests : ApiTestBase
{
    public AdminAddsAndRemovesProductTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiFact]
    public async Task Admin_AddsSeedPacket_ShoppersSeeIt_ThenAdminRemovesIt()
    {
        // Given the merchant and a Citizen are signed in
        HttpClient admin = await ClientForAsync("admin");
        HttpClient citizen = await ClientForAsync("citizen");

        // When the merchant adds a Seed Packet to the Basic shelf
        HttpResponseMessage created = await admin.PostAsJsonAsync("/api/admin/products", new
        {
            name = "Seed Packet",
            description = "Heirloom tomato seeds for the rooftop gardens.",
            categoryId = 1,
            priceCredits = 8,
            iconKey = "ration",
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        AdminProductDto product = await ReadAsync<AdminProductDto>(created);

        // Then the Citizen finds it in a search and may buy it
        List<ProductDto> found = await ReadAsync<List<ProductDto>>(await citizen.GetAsync("/api/products?search=seed"));
        ProductDto seedPacket = Assert.Single(found);
        Assert.Equal(product.ProductId, seedPacket.ProductId);
        Assert.True(seedPacket.CanPurchase);

        // When the merchant removes it
        HttpResponseMessage deleted = await admin.DeleteAsync("/api/admin/products/" + product.ProductId);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // Then it is gone from the shelves
        List<ProductDto> afterDelete = await ReadAsync<List<ProductDto>>(await citizen.GetAsync("/api/products?search=seed"));
        Assert.Empty(afterDelete);
    }
}
