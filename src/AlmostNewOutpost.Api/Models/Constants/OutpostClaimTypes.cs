namespace AlmostNewOutpost.Api.Models.Constants;

/// <summary>
/// The exact claim names written into our JWTs. The UI reads "role" and "exp" from the token.
/// </summary>
public static class OutpostClaimTypes
{
    public const string UserId = "sub";
    public const string Username = "unique_name";
    public const string Role = "role";
    public const string TokenId = "jti";
}
