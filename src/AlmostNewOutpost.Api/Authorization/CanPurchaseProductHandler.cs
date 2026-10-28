using AlmostNewOutpost.Api.Models.Constants;
using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace AlmostNewOutpost.Api.Authorization;

/// <summary>
/// Decides CanPurchaseProductRequirement: reads the role from the user's token and asks
/// CategoryAccessPolicy whether that role may buy the product's category.
/// </summary>
public class CanPurchaseProductHandler : AuthorizationHandler<CanPurchaseProductRequirement, ProductRow>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanPurchaseProductRequirement requirement,
        ProductRow resource)
    {
        string? roleName = context.User.FindFirst(OutpostClaimTypes.Role)?.Value;

        if (CategoryAccessPolicy.CanBuy(roleName, resource.CategoryName))
        {
            context.Succeed(requirement);
        }

        // Not calling Succeed means "no": the purchase is refused with 403.
        return Task.CompletedTask;
    }
}
