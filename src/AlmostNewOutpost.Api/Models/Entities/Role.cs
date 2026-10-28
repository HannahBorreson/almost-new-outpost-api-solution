namespace AlmostNewOutpost.Api.Models.Entities;

/// <summary>
/// One row of the Roles table. A role decides which product categories a user may buy.
/// </summary>
public class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
