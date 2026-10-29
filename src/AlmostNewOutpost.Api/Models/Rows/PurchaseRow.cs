namespace AlmostNewOutpost.Api.Models.Rows;

/// <summary>
/// The columns returned by sp_CreatePurchase and sp_GetPurchasesByUser.
/// </summary>
public class PurchaseRow
{
    public int PurchaseId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    /// <summary>UTC, but MySQL hands it back with DateTimeKind.Unspecified (see PurchaseDto).</summary>
    public DateTime PurchaseDate { get; set; }

    public int FinalPrice { get; set; }
}
