using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Models.Requests;

/// <summary>
/// Body of POST /api/auth/login.
/// </summary>
public class LoginRequest
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
