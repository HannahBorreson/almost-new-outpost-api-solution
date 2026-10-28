using System.Security.Claims;
using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Extensions;
using AlmostNewOutpost.Api.Models.Constants;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Repositories;
using AlmostNewOutpost.Api.Weather;
using Microsoft.AspNetCore.Authorization;

namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// Buying things. The server decides everything that matters: who is buying (from the token),
/// whether they may (CanPurchaseProduct policy), and the price (from the current weather).
/// </summary>
public class PurchaseService
{
    private readonly IProductRepository _products;
    private readonly IPurchaseRepository _purchases;
    private readonly IAuthorizationService _authorizationService;
    private readonly WeatherState _weatherState;

    public PurchaseService(
        IProductRepository products,
        IPurchaseRepository purchases,
        IAuthorizationService authorizationService,
        WeatherState weatherState)
    {
        _products = products;
        _purchases = purchases;
        _authorizationService = authorizationService;
        _weatherState = weatherState;
    }

    public async Task<PurchaseResult> PurchaseAsync(ClaimsPrincipal user, int productId)
    {
        // Identity always comes from the token, never from the request body.
        int? userId = user.GetUserId();
        if (userId == null)
        {
            return PurchaseResult.Failed(PurchaseStatus.UserNotFound);
        }

        // 1. Load the product.
        ProductRow? product = await _products.GetByIdAsync(productId);
        if (product == null)
        {
            return PurchaseResult.Failed(PurchaseStatus.ProductNotFound);
        }

        // 2. May this user buy this product? (CanPurchaseProductHandler asks CategoryAccessPolicy.)
        AuthorizationResult authorization =
            await _authorizationService.AuthorizeAsync(user, product, PolicyNames.CanPurchaseProduct);
        if (!authorization.Succeeded)
        {
            return PurchaseResult.Failed(PurchaseStatus.NotAuthorized);
        }

        // 3. Work out the price for the current weather.
        int finalPrice = PricingService.GetCurrentPrice(product.PriceCredits, _weatherState.GetSnapshot().Condition);

        // 4. Record it.
        try
        {
            PurchaseRow row = await _purchases.CreateAsync(userId.Value, productId, finalPrice);
            return PurchaseResult.Succeeded(PurchaseDto.FromRow(row));
        }
        catch (Exception exception) when (DatabaseErrors.GetErrorNumber(exception) == DatabaseErrors.ProductNotFound)
        {
            // The product was deleted between steps 1 and 4.
            return PurchaseResult.Failed(PurchaseStatus.ProductNotFound);
        }
        catch (Exception exception) when (DatabaseErrors.GetErrorNumber(exception) == DatabaseErrors.UserNotFound)
        {
            return PurchaseResult.Failed(PurchaseStatus.UserNotFound);
        }
    }

    /// <summary>The user's own purchases, newest first.</summary>
    public async Task<List<PurchaseDto>> GetPurchasesAsync(int userId, int limit)
    {
        List<PurchaseRow> rows = await _purchases.GetByUserAsync(userId, limit);
        return rows.Select(PurchaseDto.FromRow).ToList();
    }
}
