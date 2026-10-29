using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Repositories;

/// <summary>
/// Reads and writes products through the product stored procedures. The procedures raise
/// SIGNAL errors (see DatabaseErrors) when a rule is broken; those surface as exceptions.
/// </summary>
public interface IProductRepository
{
    /// <summary>sp_SearchProducts. sortBy is "name" or "price"; sortDir is "asc" or "desc".</summary>
    Task<List<ProductRow>> SearchAsync(string search, string sortBy, string sortDir);

    /// <summary>sp_GetProductById. Null if no such product.</summary>
    Task<ProductRow?> GetByIdAsync(int productId);

    /// <summary>sp_CreateProduct. Returns the new product.</summary>
    Task<ProductRow> CreateAsync(string name, string? description, int categoryId, int priceCredits, string? iconKey);

    /// <summary>sp_UpdateProduct. Returns the updated product.</summary>
    Task<ProductRow> UpdateAsync(int productId, string name, string? description, int categoryId, int priceCredits, string? iconKey);

    /// <summary>sp_DeleteProduct.</summary>
    Task DeleteAsync(int productId);
}
