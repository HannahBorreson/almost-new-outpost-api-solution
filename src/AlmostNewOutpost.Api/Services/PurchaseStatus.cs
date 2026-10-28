namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// How a purchase attempt turned out. Controllers turn each value into an HTTP status code.
/// </summary>
public enum PurchaseStatus
{
    Success,
    ProductNotFound,
    NotAuthorized,
    UserNotFound,
}
