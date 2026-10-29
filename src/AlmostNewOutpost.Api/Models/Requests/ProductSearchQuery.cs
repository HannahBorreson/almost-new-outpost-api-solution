using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Models.Requests;

/// <summary>
/// Query string of GET /api/products: ?search=&amp;sortBy=name|price&amp;sortDir=asc|desc.
/// Missing values default to name / asc in ProductService.
/// </summary>
public class ProductSearchQuery
{
    [StringLength(50, ErrorMessage = "search can be at most 50 characters.")]
    public string? Search { get; set; }

    [RegularExpression("^(?i:name|price)$", ErrorMessage = "sortBy must be \"name\" or \"price\".")]
    public string? SortBy { get; set; }

    [RegularExpression("^(?i:asc|desc)$", ErrorMessage = "sortDir must be \"asc\" or \"desc\".")]
    public string? SortDir { get; set; }
}
