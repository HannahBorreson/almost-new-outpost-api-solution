namespace AlmostNewOutpost.Api.Models.Rows;

/// <summary>
/// The columns returned by sp_GetUserByUsername. Includes the password hash, so it is only used for login.
/// Property names must match the procedure's column names exactly.
/// </summary>
public class UserRow
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
