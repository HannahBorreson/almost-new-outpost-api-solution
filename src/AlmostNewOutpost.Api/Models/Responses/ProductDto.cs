using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// A product as the shop shows it to the signed-in user: base price, price for today's
/// weather, and whether this user's role may buy it (canPurchase is for display only;
/// the purchase endpoint checks again).
/// </summary>
public class ProductDto
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int BasePrice { get; set; }

    public int CurrentPrice { get; set; }

    public bool CanPurchase { get; set; }

    public string? IconKey { get; set; }

    public static ProductDto FromRow(ProductRow row, int currentPrice, bool canPurchase)
    {
        return new ProductDto
        {
            ProductId = row.ProductId,
            Name = row.Name,
            CategoryId = row.CategoryId,
            CategoryName = row.CategoryName,
            Description = row.Description,
            BasePrice = row.PriceCredits,
            CurrentPrice = currentPrice,
            CanPurchase = canPurchase,
            IconKey = row.IconKey,
        };
    }
}
