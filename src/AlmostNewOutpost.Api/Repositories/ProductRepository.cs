using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Models.Rows;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Api.Repositories;

/// <summary>
/// Calls the product stored procedures with parameterized, interpolated SQL.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly OutpostDbContext _db;

    public ProductRepository(OutpostDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductRow>> SearchAsync(string search, string sortBy, string sortDir)
    {
        return await _db.Database
            .SqlQuery<ProductRow>($"CALL sp_SearchProducts({search}, {sortBy}, {sortDir})")
            .ToListAsync();
    }

    public async Task<ProductRow?> GetByIdAsync(int productId)
    {
        List<ProductRow> rows = await _db.Database
            .SqlQuery<ProductRow>($"CALL sp_GetProductById({productId})")
            .ToListAsync();

        return rows.FirstOrDefault();
    }

    public async Task<ProductRow> CreateAsync(string name, string? description, int categoryId, int priceCredits, string? iconKey)
    {
        // The procedure stores empty text as NULL, so sending "" for a missing value is safe.
        string descriptionValue = description ?? string.Empty;
        string iconKeyValue = iconKey ?? string.Empty;

        List<ProductRow> rows = await _db.Database
            .SqlQuery<ProductRow>($"CALL sp_CreateProduct({name}, {descriptionValue}, {categoryId}, {priceCredits}, {iconKeyValue})")
            .ToListAsync();

        return rows.First();
    }

    public async Task<ProductRow> UpdateAsync(int productId, string name, string? description, int categoryId, int priceCredits, string? iconKey)
    {
        string descriptionValue = description ?? string.Empty;
        string iconKeyValue = iconKey ?? string.Empty;

        List<ProductRow> rows = await _db.Database
            .SqlQuery<ProductRow>($"CALL sp_UpdateProduct({productId}, {name}, {descriptionValue}, {categoryId}, {priceCredits}, {iconKeyValue})")
            .ToListAsync();

        return rows.First();
    }

    public async Task DeleteAsync(int productId)
    {
        await _db.Database.ExecuteSqlAsync($"CALL sp_DeleteProduct({productId})");
    }
}
