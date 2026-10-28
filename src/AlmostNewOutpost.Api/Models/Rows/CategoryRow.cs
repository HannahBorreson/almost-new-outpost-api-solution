namespace AlmostNewOutpost.Api.Models.Rows;

/// <summary>
/// The columns returned by sp_GetCategories.
/// </summary>
public class CategoryRow
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}
