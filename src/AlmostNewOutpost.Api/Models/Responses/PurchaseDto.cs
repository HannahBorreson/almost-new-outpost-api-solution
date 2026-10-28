using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// One purchase in the signed-in user's history.
/// </summary>
public class PurchaseDto
{
    public int PurchaseId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }

    public int FinalPrice { get; set; }

    public static PurchaseDto FromRow(PurchaseRow row)
    {
        return new PurchaseDto
        {
            PurchaseId = row.PurchaseId,
            ProductId = row.ProductId,
            ProductName = row.ProductName,
            // MySQL returns DATETIME values with Kind = Unspecified. The column is always UTC,
            // so mark it as UTC; then the JSON ends in "Z" and browsers read it correctly.
            PurchaseDate = DateTime.SpecifyKind(row.PurchaseDate, DateTimeKind.Utc),
            FinalPrice = row.FinalPrice,
        };
    }
}
