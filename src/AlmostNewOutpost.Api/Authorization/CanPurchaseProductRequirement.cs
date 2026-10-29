using Microsoft.AspNetCore.Authorization;

namespace AlmostNewOutpost.Api.Authorization;

/// <summary>
/// "The user may buy this product." Checked against a specific product (resource-based
/// authorization) by CanPurchaseProductHandler.
/// </summary>
public class CanPurchaseProductRequirement : IAuthorizationRequirement
{
}
