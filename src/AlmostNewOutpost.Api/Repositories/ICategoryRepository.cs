using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Repositories;

/// <summary>
/// Reads categories through sp_GetCategories.
/// </summary>
public interface ICategoryRepository
{
    Task<List<CategoryRow>> GetAllAsync();
}
