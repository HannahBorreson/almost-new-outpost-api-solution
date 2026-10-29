using AlmostNewOutpost.Api.Models.Constants;

namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// The one and only place that says which role may buy which product category.
/// The product listing (canPurchase) and the purchase check both ask this class,
/// so they can never disagree. Pure: no database or HTTP.
/// </summary>
public static class CategoryAccessPolicy
{
    private static readonly Dictionary<string, string[]> AllowedCategoriesByRole =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Citizen] = new[] { CategoryNames.Basic },
            [RoleNames.Scout] = new[] { CategoryNames.Basic, CategoryNames.Exploration },
            [RoleNames.Medic] = new[] { CategoryNames.Basic, CategoryNames.Medical },
            [RoleNames.Security] = new[] { CategoryNames.Basic, CategoryNames.Security },
            [RoleNames.MerchantAdmin] = new[]
            {
                CategoryNames.Basic, CategoryNames.Exploration, CategoryNames.Medical, CategoryNames.Security,
            },
        };

    /// <summary>
    /// True if a user with this role may buy products in this category.
    /// An unknown role or category means no.
    /// </summary>
    public static bool CanBuy(string? roleName, string? categoryName)
    {
        if (string.IsNullOrEmpty(roleName) || string.IsNullOrEmpty(categoryName))
        {
            return false;
        }

        if (!AllowedCategoriesByRole.TryGetValue(roleName, out string[]? allowedCategories))
        {
            return false;
        }

        return allowedCategories.Contains(categoryName, StringComparer.OrdinalIgnoreCase);
    }
}
