using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Options;

/// <summary>
/// Which browser origins may call the API, bound from the "Cors" section
/// (only the React dev server, http://localhost:5173).
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    public const string PolicyName = "OutpostUi";

    [Required]
    [MinLength(1)]
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
