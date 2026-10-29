namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// A user as an admin sees them, including whether they are archived.
/// It never includes the password hash.
/// </summary>
public class AdminUserDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string Role { get; set; } = string.Empty;

    /// <summary>Null means the user is active. A value means they were archived at that time (UTC).</summary>
    public DateTime? ArchivedAtUtc { get; set; }
}
