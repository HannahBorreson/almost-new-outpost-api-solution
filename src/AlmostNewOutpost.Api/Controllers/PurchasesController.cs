using System.ComponentModel.DataAnnotations;
using AlmostNewOutpost.Api.Extensions;
using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Api.Controllers;

/// <summary>
/// Buying products and reading your own purchase history. The user always comes from the
/// token, so nobody can buy as, or read the history of, someone else.
/// </summary>
[ApiController]
[Route("api/purchases")]
public class PurchasesController : ControllerBase
{
    private readonly PurchaseService _purchaseService;

    public PurchasesController(PurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    /// <summary>Buy one product at the server-calculated price.</summary>
    [HttpPost]
    [ProducesResponseType<PurchaseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseDto>> Create(CreatePurchaseRequest request)
    {
        PurchaseResult result = await _purchaseService.PurchaseAsync(User, request.ProductId ?? 0);

        switch (result.Status)
        {
            case PurchaseStatus.Success:
                // There's no "get one purchase" endpoint, so no Location header.
                return StatusCode(StatusCodes.Status201Created, result.Purchase);
            case PurchaseStatus.ProductNotFound:
                return Problem(title: "Product not found.", statusCode: StatusCodes.Status404NotFound);
            case PurchaseStatus.NotAuthorized:
                return Problem(title: "You are not authorized to purchase this product.", statusCode: StatusCodes.Status403Forbidden);
            default:
                return Problem(title: "Your account could not be found. Please log in again.", statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>Your purchases, newest first.</summary>
    [HttpGet("me")]
    [ProducesResponseType<List<PurchaseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PurchaseDto>>> GetMine([FromQuery, Range(1, 100)] int limit = 50)
    {
        int? userId = User.GetUserId();
        if (userId == null)
        {
            return Problem(title: "Your account could not be found. Please log in again.", statusCode: StatusCodes.Status401Unauthorized);
        }

        List<PurchaseDto> purchases = await _purchaseService.GetPurchasesAsync(userId.Value, limit);
        return Ok(purchases);
    }
}
