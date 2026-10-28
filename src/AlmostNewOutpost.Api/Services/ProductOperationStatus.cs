namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// How an admin product create, update or delete turned out. Controllers turn each value
/// into an HTTP status code.
/// </summary>
public enum ProductOperationStatus
{
    Success,
    NotFound,
    CategoryNotFound,
    DuplicateName,
    HasPurchases,
}
