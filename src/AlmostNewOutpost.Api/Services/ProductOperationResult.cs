using AlmostNewOutpost.Api.Models.Responses;

namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// The outcome of an admin product change: a status, and the product when it succeeded.
/// </summary>
public class ProductOperationResult
{
    public ProductOperationStatus Status { get; init; }

    public AdminProductDto? Product { get; init; }

    public static ProductOperationResult Succeeded(AdminProductDto? product)
    {
        return new ProductOperationResult { Status = ProductOperationStatus.Success, Product = product };
    }

    public static ProductOperationResult Failed(ProductOperationStatus status)
    {
        return new ProductOperationResult { Status = status };
    }
}
