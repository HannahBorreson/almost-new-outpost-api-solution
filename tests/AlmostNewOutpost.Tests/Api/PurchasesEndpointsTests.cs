using System.Net;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Models.Entities;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Tests.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Tests.Api;

/// <summary>
/// POST /api/purchases and GET /api/purchases/me.
/// </summary>
[Collection(ApiCollection.Name)]
public class PurchasesEndpointsTests : ApiTestBase
{
    public PurchasesEndpointsTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiFact]
    public async Task Medic_BuysMedical_Returns201_AndStoresTheRowAtTheCurrentPrice()
    {
        HttpClient client = await ClientForAsync("medic");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/purchases", new { productId = 12 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PurchaseDto purchase = await ReadAsync<PurchaseDto>(response);
        Assert.Equal(12, purchase.ProductId);
        Assert.Equal("Medical Supply Kit", purchase.ProductName);
        Assert.Equal(100, purchase.FinalPrice);

        Purchase? stored = await WithDatabaseAsync(db => db.Purchases.SingleOrDefaultAsync(p => p.PurchaseId == purchase.PurchaseId));
        Assert.NotNull(stored);
        Assert.Equal(3, stored.UserId);
        Assert.Equal(100, stored.FinalPrice);
    }

    [ApiFact]
    public async Task PurchaseDate_IsUtcIsoWithZ()
    {
        HttpClient client = await ClientForAsync("medic");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/purchases", new { productId = 13 });
        string json = await response.Content.ReadAsStringAsync();

        Assert.Matches("\"purchaseDate\":\"[0-9T:.\\-]+Z\"", json);
    }

    [ApiFact]
    public async Task Citizen_BuysSecurityItem_Returns403_AndNothingIsStored()
    {
        HttpClient client = await ClientForAsync("citizen");
        int before = await WithDatabaseAsync(db => db.Purchases.CountAsync());

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/purchases", new { productId = 15 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        ProblemDetails problem = await ReadAsync<ProblemDetails>(response);
        Assert.Equal("You are not authorized to purchase this product.", problem.Title);
        int after = await WithDatabaseAsync(db => db.Purchases.CountAsync());
        Assert.Equal(before, after);
    }

    [ApiFact]
    public async Task UnknownProduct_Returns404()
    {
        HttpClient client = await ClientForAsync("admin");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/purchases", new { productId = 999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [ApiFact]
    public async Task MissingOrInvalidProductId_Returns400()
    {
        HttpClient client = await ClientForAsync("admin");

        HttpResponseMessage missing = await client.PostAsJsonAsync("/api/purchases", new { });
        HttpResponseMessage zero = await client.PostAsJsonAsync("/api/purchases", new { productId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
    }

    [ApiFact]
    public async Task ExtraFields_LikeFinalPrice_AreIgnored()
    {
        HttpClient client = await ClientForAsync("medic");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/purchases", new { productId = 12, finalPrice = 1, userId = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PurchaseDto purchase = await ReadAsync<PurchaseDto>(response);
        Assert.Equal(100, purchase.FinalPrice);
        Purchase? stored = await WithDatabaseAsync(db => db.Purchases.SingleOrDefaultAsync(p => p.PurchaseId == purchase.PurchaseId));
        Assert.NotNull(stored);
        Assert.Equal(3, stored.UserId); // the medic, from the token, not userId 1 from the body
    }

    [ApiFact]
    public async Task Mine_ReturnsOnlyTheCallersRows_NewestFirst()
    {
        HttpClient client = await ClientForAsync("medic");

        List<PurchaseDto> purchases = await ReadAsync<List<PurchaseDto>>(await client.GetAsync("/api/purchases/me"));

        Assert.Equal(new[] { "Medical Supply Kit", "Bandages", "First Aid Kit" }, purchases.Select(p => p.ProductName));
        List<int> medicProductIds = await WithDatabaseAsync(db =>
            db.Purchases.Where(p => p.UserId == 3).Select(p => p.ProductId).ToListAsync());
        Assert.All(purchases, p => Assert.Contains(p.ProductId, medicProductIds));
    }

    [ApiFact]
    public async Task Mine_RespectsTheLimit()
    {
        HttpClient client = await ClientForAsync("medic");

        List<PurchaseDto> purchases = await ReadAsync<List<PurchaseDto>>(await client.GetAsync("/api/purchases/me?limit=2"));

        Assert.Equal(2, purchases.Count);
    }

    [ApiTheory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Mine_LimitOutOfRange_Returns400(int limit)
    {
        HttpClient client = await ClientForAsync("medic");

        HttpResponseMessage response = await client.GetAsync("/api/purchases/me?limit=" + limit);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [ApiFact]
    public async Task Mine_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await AnonymousClient().GetAsync("/api/purchases/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
