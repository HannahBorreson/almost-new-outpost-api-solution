using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Models.Constants;
using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Repositories;
using AlmostNewOutpost.Api.Weather;

namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// Product listing and management. Adds the two things the database doesn't know:
/// the price for today's weather, and whether the caller's role may buy each product.
/// </summary>
public class ProductService
{
    private readonly IProductRepository _products;
    private readonly WeatherState _weatherState;

    public ProductService(IProductRepository products, WeatherState weatherState)
    {
        _products = products;
        _weatherState = weatherState;
    }

    /// <summary>
    /// Every product matching the search, for every role. Returns null only if the database
    /// rejects the sort values (validation normally stops that first).
    /// </summary>
    public async Task<List<ProductDto>?> SearchAsync(ProductSearchQuery query, string? roleName)
    {
        string search = (query.Search ?? string.Empty).Trim();
        string sortBy = (query.SortBy ?? "name").ToLowerInvariant();
        string sortDir = (query.SortDir ?? "asc").ToLowerInvariant();

        List<ProductRow> rows;
        try
        {
            rows = await _products.SearchAsync(search, sortBy, sortDir);
        }
        catch (Exception exception) when (IsInvalidSort(exception))
        {
            return null;
        }

        WeatherCondition condition = _weatherState.GetSnapshot().Condition;
        return rows.Select(row => ToProductDto(row, condition, roleName)).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int productId, string? roleName)
    {
        ProductRow? row = await _products.GetByIdAsync(productId);
        if (row == null)
        {
            return null;
        }

        WeatherCondition condition = _weatherState.GetSnapshot().Condition;
        return ToProductDto(row, condition, roleName);
    }

    /// <summary>All products for the admin page, sorted by name.</summary>
    public async Task<List<AdminProductDto>> GetAllForAdminAsync()
    {
        List<ProductRow> rows = await _products.SearchAsync(string.Empty, "name", "asc");
        WeatherCondition condition = _weatherState.GetSnapshot().Condition;
        return rows.Select(row => ToAdminDto(row, condition)).ToList();
    }

    public async Task<ProductOperationResult> CreateAsync(ProductUpsertRequest request)
    {
        try
        {
            ProductRow row = await _products.CreateAsync(
                request.Name.Trim(),
                request.Description,
                request.CategoryId ?? 0,
                request.PriceCredits ?? 0,
                request.IconKey);
            return ProductOperationResult.Succeeded(ToAdminDto(row, _weatherState.GetSnapshot().Condition));
        }
        catch (Exception exception) when (ToStatus(exception) != null)
        {
            return ProductOperationResult.Failed(ToStatus(exception)!.Value);
        }
    }

    public async Task<ProductOperationResult> UpdateAsync(int productId, ProductUpsertRequest request)
    {
        try
        {
            ProductRow row = await _products.UpdateAsync(
                productId,
                request.Name.Trim(),
                request.Description,
                request.CategoryId ?? 0,
                request.PriceCredits ?? 0,
                request.IconKey);
            return ProductOperationResult.Succeeded(ToAdminDto(row, _weatherState.GetSnapshot().Condition));
        }
        catch (Exception exception) when (ToStatus(exception) != null)
        {
            return ProductOperationResult.Failed(ToStatus(exception)!.Value);
        }
    }

    public async Task<ProductOperationResult> DeleteAsync(int productId)
    {
        try
        {
            await _products.DeleteAsync(productId);
            return ProductOperationResult.Succeeded(null);
        }
        catch (Exception exception) when (ToStatus(exception) != null)
        {
            return ProductOperationResult.Failed(ToStatus(exception)!.Value);
        }
    }

    private static ProductDto ToProductDto(ProductRow row, WeatherCondition condition, string? roleName)
    {
        int currentPrice = PricingService.GetCurrentPrice(row.PriceCredits, condition);
        bool canPurchase = CategoryAccessPolicy.CanBuy(roleName, row.CategoryName);
        return ProductDto.FromRow(row, currentPrice, canPurchase);
    }

    private static AdminProductDto ToAdminDto(ProductRow row, WeatherCondition condition)
    {
        int currentPrice = PricingService.GetCurrentPrice(row.PriceCredits, condition);
        return AdminProductDto.FromRow(row, currentPrice);
    }

    /// <summary>
    /// Maps a stored-procedure error to a product status. Returns null for any other error,
    /// which is then left to become a 500.
    /// </summary>
    private static ProductOperationStatus? ToStatus(Exception exception)
    {
        switch (DatabaseErrors.GetErrorNumber(exception))
        {
            case DatabaseErrors.ProductNotFound:
                return ProductOperationStatus.NotFound;
            case DatabaseErrors.CategoryNotFound:
                return ProductOperationStatus.CategoryNotFound;
            case DatabaseErrors.DuplicateProductName:
            case DatabaseErrors.DuplicateKey:
                return ProductOperationStatus.DuplicateName;
            case DatabaseErrors.ProductHasPurchases:
                return ProductOperationStatus.HasPurchases;
            default:
                return null;
        }
    }

    private static bool IsInvalidSort(Exception exception)
    {
        int? number = DatabaseErrors.GetErrorNumber(exception);
        return number == DatabaseErrors.InvalidSortBy || number == DatabaseErrors.InvalidSortDir;
    }
}
