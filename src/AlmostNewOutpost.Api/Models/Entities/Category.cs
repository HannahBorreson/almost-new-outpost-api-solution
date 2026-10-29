namespace AlmostNewOutpost.Api.Models.Entities;

/// <summary>
/// One row of the Categories table (Basic, Exploration, Medical, Security).
/// </summary>
public class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}
