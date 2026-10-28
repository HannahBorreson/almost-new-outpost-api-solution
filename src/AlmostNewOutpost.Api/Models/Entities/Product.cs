namespace AlmostNewOutpost.Api.Models.Entities;

/// <summary>
/// One row of the Products table. PriceCredits is the base (clear weather) price.
/// </summary>
public class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int PriceCredits { get; set; }

    public string? IconKey { get; set; }
}
