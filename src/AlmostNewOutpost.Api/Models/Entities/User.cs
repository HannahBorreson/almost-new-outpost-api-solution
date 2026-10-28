namespace AlmostNewOutpost.Api.Models.Entities;

/// <summary>
/// One row of the Users table: someone who can log in. Each user has exactly one role.
/// </summary>
public class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
}
