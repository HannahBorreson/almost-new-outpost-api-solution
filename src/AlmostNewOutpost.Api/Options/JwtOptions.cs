using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Options;

/// <summary>
/// Settings for issuing and checking JWTs, bound from the "Jwt" section.
/// Issuer, audience and expiry are in appsettings.json; SigningKey comes from user-secrets.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 needs a key of at least 256 bits.</summary>
    public const int MinimumKeyBytes = 32;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;

    [Required(ErrorMessage = "Jwt:SigningKey is missing. Set it with: dotnet user-secrets set \"Jwt:SigningKey\" \"<64 random characters>\" (see README).")]
    public string SigningKey { get; set; } = string.Empty;
}
