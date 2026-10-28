using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// What the admin product endpoints return: every ProductDto field, plus priceCredits.
/// The UI's Manage Products form reads and edits "priceCredits" (the database column
/// and the request field), so admin responses include it alongside basePrice.
/// </summary>
public class AdminProductDto : ProductDto
{
    public int PriceCredits { get; set; }

    public static AdminProductDto FromRow(ProductRow row, int currentPrice)
    {
        return new AdminProductDto
        {
            ProductId = row.ProductId,
            Name = row.Name,
            CategoryId = row.CategoryId,
            CategoryName = row.CategoryName,
            Description = row.Description,
            BasePrice = row.PriceCredits,
            CurrentPrice = currentPrice,
            CanPurchase = true, // admins may buy every category
            IconKey = row.IconKey,
            PriceCredits = row.PriceCredits,
        };
    }
}
