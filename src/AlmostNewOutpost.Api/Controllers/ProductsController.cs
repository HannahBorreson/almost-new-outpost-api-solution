using AlmostNewOutpost.Api.Extensions;
using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Api.Controllers;

/// <summary>
/// The shop shelves. Every role sees every product; canPurchase says whether this user may buy it.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Search by name and sort by name or price. The database does the searching and sorting.</summary>
    [HttpGet]
    [ProducesResponseType<List<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ProductDto>>> Search([FromQuery] ProductSearchQuery query)
    {
        List<ProductDto>? products = await _productService.SearchAsync(query, User.GetRoleName());
        if (products == null)
        {
            ModelState.AddModelError(nameof(ProductSearchQuery.SortBy), "sortBy must be \"name\" or \"price\" and sortDir must be \"asc\" or \"desc\".");
            return ValidationProblem(ModelState);
        }

        return Ok(products);
    }

    /// <summary>One product, for the details window.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        ProductDto? product = await _productService.GetByIdAsync(id, User.GetRoleName());
        if (product == null)
        {
            return Problem(title: "Product not found.", statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(product);
    }
}
