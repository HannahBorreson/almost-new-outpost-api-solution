using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// A product category: { "categoryId": 1, "categoryName": "Basic" }.
/// </summary>
public class CategoryDto
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public static CategoryDto FromRow(CategoryRow row)
    {
        return new CategoryDto { CategoryId = row.CategoryId, CategoryName = row.CategoryName };
    }
}
