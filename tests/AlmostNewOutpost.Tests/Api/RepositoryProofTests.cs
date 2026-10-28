using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Repositories;
using AlmostNewOutpost.Tests.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AlmostNewOutpost.Tests.Api;

/// <summary>
/// Proves the data-access approach works before anything is built on it: EF Core's
/// Database.SqlQuery can CALL a stored procedure through Oracle's MySQL provider.
/// </summary>
[Collection(ApiCollection.Name)]
public class RepositoryProofTests : ApiTestBase
{
    public RepositoryProofTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiFact]
    public async Task SearchProducts_CallsTheStoredProcedure()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        IProductRepository products = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        List<ProductRow> all = await products.SearchAsync(string.Empty, "name", "asc");
        List<ProductRow> kits = await products.SearchAsync("kit", "price", "asc");

        Assert.Equal(18, all.Count);
        Assert.Equal(
            new[] { "Cooking Kit", "First Aid Kit", "Signal Flare Kit", "Medical Supply Kit" },
            kits.Select(p => p.Name));
        Assert.Equal("Medical", kits[3].CategoryName);
    }

    [ApiFact]
    public async Task PurchaseDates_ComeBackFromMySql()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        IPurchaseRepository purchases = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();

        List<PurchaseRow> medicPurchases = await purchases.GetByUserAsync(3, 50);

        Assert.Equal(3, medicPurchases.Count);
        Assert.Equal("Medical Supply Kit", medicPurchases[0].ProductName); // newest first
    }
}
