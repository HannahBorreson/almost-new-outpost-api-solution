using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Repositories;

/// <summary>
/// Writes and reads purchases through the purchase stored procedures.
/// </summary>
public interface IPurchaseRepository
{
    /// <summary>sp_CreatePurchase. The caller has already checked permission and worked out the price.</summary>
    Task<PurchaseRow> CreateAsync(int userId, int productId, int finalPrice);

    /// <summary>sp_GetPurchasesByUser: one user's purchases, newest first.</summary>
    Task<List<PurchaseRow>> GetByUserAsync(int userId, int limit);
}
