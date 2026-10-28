using System.Net;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Tests.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Tests.Api;

/// <summary>
/// GET /api/products, GET /api/products/{id} and GET /api/categories.
/// </summary>
[Collection(ApiCollection.Name)]
public class ProductsEndpointsTests : ApiTestBase
{
    public ProductsEndpointsTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiFact]
    public async Task Medic_SeesEveryProduct_CanPurchaseOnlyBasicAndMedical()
    {
        HttpClient client = await ClientForAsync("medic");

        List<ProductDto> products = await ReadAsync<List<ProductDto>>(await client.GetAsync("/api/products"));

        Assert.Equal(18, products.Count);
        foreach (ProductDto product in products)
        {
            bool expected = product.CategoryName == "Basic" || product.CategoryName == "Medical";
            Assert.Equal(expected, product.CanPurchase);
            Assert.Equal(product.BasePrice, product.CurrentPrice); // the test weather is Clear
        }
    }

    [ApiFact]
    public async Task Search_Kit_SortedByPriceAscending()
    {
        HttpClient client = await ClientForAsync("citizen");

        List<ProductDto> products = await ReadAsync<List<ProductDto>>(
            await client.GetAsync("/api/products?search=kit&sortBy=price&sortDir=asc"));

        Assert.Equal(
            new[] { "Cooking Kit", "First Aid Kit", "Signal Flare Kit", "Medical Supply Kit" },
            products.Select(p => p.Name));
    }

    [ApiFact]
    public async Task Search_SortByNameDescending_IsCaseInsensitive()
    {
        HttpClient client = await ClientForAsync("citizen");

        List<ProductDto> products = await ReadAsync<List<ProductDto>>(
            await client.GetAsync("/api/products?sortBy=NAME&sortDir=Desc"));

        Assert.Equal("Survival Backpack", products[0].Name);
    }

    [ApiTheory]
    [InlineData("/api/products?sortBy=category")]
    [InlineData("/api/products?sortDir=sideways")]
    public async Task Search_BadSortValues_Return400(string url)
    {
        HttpClient client = await ClientForAsync("citizen");

        HttpResponseMessage response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await ReadAsync<ValidationProblemDetails>(response);
    }

    [ApiFact]
    public async Task Search_LongerThan50Characters_Returns400()
    {
        HttpClient client = await ClientForAsync("citizen");

        HttpResponseMessage response = await client.GetAsync("/api/products?search=" + new string('a', 51));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [ApiFact]
    public async Task GetById_ReturnsTheProductShape()
    {
        HttpClient client = await ClientForAsync("medic");

        ProductDto product = await ReadAsync<ProductDto>(await client.GetAsync("/api/products/12"));

        Assert.Equal(12, product.ProductId);
        Assert.Equal("Medical Supply Kit", product.Name);
        Assert.Equal(3, product.CategoryId);
        Assert.Equal("Medical", product.CategoryName);
        Assert.Equal("Sterile dressings, splints and basic instruments.", product.Description);
        Assert.Equal(100, product.BasePrice);
        Assert.Equal(100, product.CurrentPrice);
        Assert.True(product.CanPurchase);
        Assert.Equal("medical-kit", product.IconKey);
    }

    [ApiFact]
    public async Task GetById_Unknown_Returns404()
    {
        HttpClient client = await ClientForAsync("medic");

        HttpResponseMessage response = await client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [ApiFact]
    public async Task Categories_ReturnsTheFour()
    {
        HttpClient client = await ClientForAsync("scout");

        List<CategoryDto> categories = await ReadAsync<List<CategoryDto>>(await client.GetAsync("/api/categories"));

        Assert.Equal(new[] { "Basic", "Exploration", "Medical", "Security" }, categories.Select(c => c.CategoryName));
    }

    [ApiFact]
    public async Task Categories_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await AnonymousClient().GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
