namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// Returned by a successful login: the JWT, when it expires, and who logged in.
/// </summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public UserDto User { get; set; } = new UserDto();
}
