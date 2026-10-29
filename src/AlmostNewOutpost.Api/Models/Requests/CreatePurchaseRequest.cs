using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Models.Requests;

/// <summary>
/// Body of POST /api/purchases. Only the product id is accepted: the server decides
/// the price, the user and the date, and any other JSON fields are ignored.
/// </summary>
public class CreatePurchaseRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "productId must be 1 or more.")]
    public int? ProductId { get; set; }
}
