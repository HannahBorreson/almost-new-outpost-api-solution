namespace AlmostNewOutpost.Api.Models.Entities;

/// <summary>
/// One row of the Purchases table. FinalPrice is what was actually charged, so history never changes.
/// </summary>
public class Purchase
{
    public int PurchaseId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    /// <summary>Stored in UTC.</summary>
    public DateTime PurchaseDate { get; set; }

    public int FinalPrice { get; set; }
}
