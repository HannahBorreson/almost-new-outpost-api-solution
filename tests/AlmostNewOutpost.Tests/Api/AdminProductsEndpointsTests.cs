using System.Net;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Tests.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Tests.Api;

/// <summary>
/// /api/admin/products: list, create, update, delete, and who may use them.
/// </summary>
[Collection(ApiCollection.Name)]
public class AdminProductsEndpointsTests : ApiTestBase
{
    public AdminProductsEndpointsTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    private static object NewProduct(string name = "Seed Packet", int categoryId = 1, int priceCredits = 8)
    {
        return new
        {
            name,
            description = "Heirloom tomato seeds for the rooftop gardens.",
            categoryId,
            priceCredits,
            iconKey = "ration",
        };
    }

    [ApiFact]
    public async Task Medic_CallingAdminRoutes_Gets403()
    {
        HttpClient medic = await ClientForAsync("medic");

        HttpResponseMessage list = await medic.GetAsync("/api/admin/products");
        HttpResponseMessage create = await medic.PostAsJsonAsync("/api/admin/products", NewProduct());
        HttpResponseMessage delete = await medic.DeleteAsync("/api/admin/products/4");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        ProblemDetails problem = await ReadAsync<ProblemDetails>(list);
        Assert.Equal(403, problem.Status);
    }

    [ApiFact]
    public async Task List_ReturnsEveryProductSortedByName()
    {
        HttpClient admin = await ClientForAsync("admin");

        List<AdminProductDto> products = await ReadAsync<List<AdminProductDto>>(await admin.GetAsync("/api/admin/products"));

        Assert.Equal(18, products.Count);
        Assert.Equal(products.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase), products.Select(p => p.Name));
        Assert.All(products, p => Assert.Equal(p.BasePrice, p.PriceCredits));
    }

    [ApiFact]
    public async Task Create_Returns201WithLocation()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/admin/products", NewProduct());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        AdminProductDto created = await ReadAsync<AdminProductDto>(response);
        Assert.Equal("Seed Packet", created.Name);
        Assert.Equal("Basic", created.CategoryName);
        Assert.Equal(8, created.PriceCredits);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith("/api/products/" + created.ProductId, response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [ApiFact]
    public async Task Create_DuplicateName_Returns409()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/admin/products", NewProduct(name: "Compass"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        ProblemDetails problem = await ReadAsync<ProblemDetails>(response);
        Assert.Equal("A product with this name already exists.", problem.Title);
    }

    [ApiFact]
    public async Task Create_InvalidFields_Returns400WithFieldErrors()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/admin/products", new
        {
            name = "",
            categoryId = 1,
            priceCredits = 0,
            iconKey = "not valid!",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains("Name", problem.Errors.Keys);
        Assert.Contains("PriceCredits", problem.Errors.Keys);
        Assert.Contains("IconKey", problem.Errors.Keys);
    }

    [ApiFact]
    public async Task Create_UnknownCategory_Returns400ForCategoryId()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/admin/products", NewProduct(categoryId: 99));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains("CategoryId", problem.Errors.Keys);
    }

    [ApiFact]
    public async Task Update_Returns200WithTheNewValues()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.PutAsJsonAsync("/api/admin/products/4", NewProduct(name: "Rechargeable Batteries", priceCredits: 12));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AdminProductDto updated = await ReadAsync<AdminProductDto>(response);
        Assert.Equal(4, updated.ProductId);
        Assert.Equal("Rechargeable Batteries", updated.Name);
        Assert.Equal(12, updated.BasePrice);
    }

    [ApiFact]
    public async Task Update_UnknownProduct_Returns404()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.PutAsJsonAsync("/api/admin/products/999", NewProduct());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [ApiFact]
    public async Task Update_ToAnotherProductsName_Returns409()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.PutAsJsonAsync("/api/admin/products/4", NewProduct(name: "Compass"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [ApiFact]
    public async Task Delete_ProductWithoutPurchases_Returns204()
    {
        HttpClient admin = await ClientForAsync("admin");

        // Flashlight (3) has never been bought in the seed data.
        HttpResponseMessage response = await admin.DeleteAsync("/api/admin/products/3");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        bool stillThere = await WithDatabaseAsync(db => db.Products.AnyAsync(p => p.ProductId == 3));
        Assert.False(stillThere);
    }

    [ApiFact]
    public async Task Delete_ProductWithPurchases_Returns409()
    {
        HttpClient admin = await ClientForAsync("admin");

        // Clean Water (1) was bought by the citizen in the seed data.
        HttpResponseMessage response = await admin.DeleteAsync("/api/admin/products/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        ProblemDetails problem = await ReadAsync<ProblemDetails>(response);
        Assert.Equal("This product has purchase history and can't be deleted.", problem.Title);
    }

    [ApiFact]
    public async Task Delete_UnknownProduct_Returns404()
    {
        HttpClient admin = await ClientForAsync("admin");

        HttpResponseMessage response = await admin.DeleteAsync("/api/admin/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
