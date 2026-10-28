using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// Who is signed in: { "id": 3, "username": "medic", "role": "Medic" }. Never includes the password hash.
/// </summary>
public class UserDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public static UserDto FromRow(UserRow row)
    {
        return new UserDto { Id = row.UserId, Username = row.Username, Role = row.RoleName };
    }

    public static UserDto FromRow(UserSummaryRow row)
    {
        return new UserDto { Id = row.UserId, Username = row.Username, Role = row.RoleName };
    }
}
