namespace AlmostNewOutpost.Api.Models.Rows;

/// <summary>
/// The columns every product procedure returns (they all read from vw_ProductDetails).
/// Property names must match the procedure's column names exactly.
/// </summary>
public class ProductRow
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int PriceCredits { get; set; }

    public string? IconKey { get; set; }
}
