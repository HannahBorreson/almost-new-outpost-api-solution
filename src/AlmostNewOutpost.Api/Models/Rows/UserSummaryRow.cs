namespace AlmostNewOutpost.Api.Models.Rows;

/// <summary>
/// The columns returned by sp_GetUserById. It has no password hash on purpose,
/// so it gets its own class instead of reusing UserRow.
/// </summary>
public class UserSummaryRow
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
