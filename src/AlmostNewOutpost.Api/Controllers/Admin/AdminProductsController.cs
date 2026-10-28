using AlmostNewOutpost.Api.Models.Constants;
using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Api.Controllers.Admin;

/// <summary>
/// Create, edit and delete products. MerchantAdmin only: anyone else gets 403.
/// </summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public AdminProductsController(ProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Every product, sorted by name.</summary>
    [HttpGet]
    [ProducesResponseType<List<AdminProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminProductDto>>> GetAll()
    {
        return Ok(await _productService.GetAllForAdminAsync());
    }

    [HttpPost]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminProductDto>> Create(ProductUpsertRequest request)
    {
        ProductOperationResult result = await _productService.CreateAsync(request);
        if (result.Status != ProductOperationStatus.Success || result.Product == null)
        {
            return ToErrorResult(result.Status);
        }

        // 201 with a Location header pointing at GET /api/products/{id}.
        return CreatedAtAction(
            nameof(ProductsController.GetById),
            "Products",
            new { id = result.Product.ProductId },
            result.Product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminProductDto>> Update(int id, ProductUpsertRequest request)
    {
        ProductOperationResult result = await _productService.UpdateAsync(id, request);
        if (result.Status != ProductOperationStatus.Success || result.Product == null)
        {
            return ToErrorResult(result.Status);
        }

        return Ok(result.Product);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        ProductOperationResult result = await _productService.DeleteAsync(id);
        if (result.Status != ProductOperationStatus.Success)
        {
            return ToErrorResult(result.Status);
        }

        return NoContent();
    }

    private ActionResult ToErrorResult(ProductOperationStatus status)
    {
        switch (status)
        {
            case ProductOperationStatus.NotFound:
                return Problem(title: "Product not found.", statusCode: StatusCodes.Status404NotFound);
            case ProductOperationStatus.CategoryNotFound:
                ModelState.AddModelError(nameof(ProductUpsertRequest.CategoryId), "Choose a category that exists.");
                return ValidationProblem(ModelState);
            case ProductOperationStatus.DuplicateName:
                return Problem(title: "A product with this name already exists.", statusCode: StatusCodes.Status409Conflict);
            case ProductOperationStatus.HasPurchases:
                return Problem(title: "This product has purchase history and can't be deleted.", statusCode: StatusCodes.Status409Conflict);
            default:
                return Problem(title: "Something went wrong.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
