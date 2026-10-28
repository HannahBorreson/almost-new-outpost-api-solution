using AlmostNewOutpost.Api.Models.Responses;

namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// The outcome of a purchase attempt: a status, and the purchase when it succeeded.
/// </summary>
public class PurchaseResult
{
    public PurchaseStatus Status { get; init; }

    public PurchaseDto? Purchase { get; init; }

    public static PurchaseResult Succeeded(PurchaseDto purchase)
    {
        return new PurchaseResult { Status = PurchaseStatus.Success, Purchase = purchase };
    }

    public static PurchaseResult Failed(PurchaseStatus status)
    {
        return new PurchaseResult { Status = status };
    }
}
