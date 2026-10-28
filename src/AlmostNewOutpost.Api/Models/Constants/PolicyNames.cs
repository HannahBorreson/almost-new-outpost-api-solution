namespace AlmostNewOutpost.Api.Models.Constants;

/// <summary>
/// Names of the authorization policies registered in AddOutpostAuth.
/// </summary>
public static class PolicyNames
{
    /// <summary>Only MerchantAdmin users.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Resource-based: may this user buy this product's category?</summary>
    public const string CanPurchaseProduct = "CanPurchaseProduct";
}
