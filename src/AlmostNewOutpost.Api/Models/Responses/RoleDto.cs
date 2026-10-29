namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// A role: { "roleId": 3, "roleName": "Medic" }.
/// </summary>
public class RoleDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
