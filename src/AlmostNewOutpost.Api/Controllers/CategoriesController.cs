using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Api.Controllers;

/// <summary>
/// The product categories (used by the admin product form's dropdown).
/// </summary>
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categories;

    public CategoriesController(ICategoryRepository categories)
    {
        _categories = categories;
    }

    [HttpGet]
    [ProducesResponseType<List<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        List<CategoryRow> rows = await _categories.GetAllAsync();
        return Ok(rows.Select(CategoryDto.FromRow).ToList());
    }
}
