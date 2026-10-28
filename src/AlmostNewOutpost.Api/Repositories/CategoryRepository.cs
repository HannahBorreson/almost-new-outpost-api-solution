using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Models.Rows;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Api.Repositories;

/// <summary>
/// Calls sp_GetCategories.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly OutpostDbContext _db;

    public CategoryRepository(OutpostDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryRow>> GetAllAsync()
    {
        return await _db.Database
            .SqlQuery<CategoryRow>($"CALL sp_GetCategories()")
            .ToListAsync();
    }
}
