using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Models.Rows;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Api.Repositories;

/// <summary>
/// Calls the purchase stored procedures.
/// </summary>
public class PurchaseRepository : IPurchaseRepository
{
    private readonly OutpostDbContext _db;

    public PurchaseRepository(OutpostDbContext db)
    {
        _db = db;
    }

    public async Task<PurchaseRow> CreateAsync(int userId, int productId, int finalPrice)
    {
        List<PurchaseRow> rows = await _db.Database
            .SqlQuery<PurchaseRow>($"CALL sp_CreatePurchase({userId}, {productId}, {finalPrice})")
            .ToListAsync();

        return rows.First();
    }

    public async Task<List<PurchaseRow>> GetByUserAsync(int userId, int limit)
    {
        return await _db.Database
            .SqlQuery<PurchaseRow>($"CALL sp_GetPurchasesByUser({userId}, {limit})")
            .ToListAsync();
    }
}
