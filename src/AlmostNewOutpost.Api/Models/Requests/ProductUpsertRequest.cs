using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Models.Requests;

/// <summary>
/// Body of POST and PUT /api/admin/products. Whether the category exists is checked
/// against the database by ProductService.
/// </summary>
public class ProductUpsertRequest : IValidatableObject
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    public int? CategoryId { get; set; }

    [Required]
    [Range(1, 100000, ErrorMessage = "Price must be a whole number from 1 to 100,000.")]
    public int? PriceCredits { get; set; }

    [StringLength(50)]
    [RegularExpression("^[A-Za-z0-9-]*$", ErrorMessage = "Icon key can only use letters, digits and hyphens.")]
    public string? IconKey { get; set; }

    /// <summary>
    /// Runs after the attribute checks pass. The name is stored trimmed, so its length
    /// is checked after trimming too.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        int trimmedLength = Name.Trim().Length;
        if (trimmedLength < 2 || trimmedLength > 100)
        {
            yield return new ValidationResult(
                "Name must be between 2 and 100 characters.",
                new[] { nameof(Name) });
        }
    }
}
