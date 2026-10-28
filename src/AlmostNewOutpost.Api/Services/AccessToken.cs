namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// A freshly issued JWT and the moment it stops working.
/// </summary>
public class AccessToken
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
