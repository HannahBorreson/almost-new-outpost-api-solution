using System.Security.Claims;
using AlmostNewOutpost.Api.Models.Constants;

namespace AlmostNewOutpost.Api.Extensions;

/// <summary>
/// Helpers for reading our claims from the signed-in user's token.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The user id from the "sub" claim, or null if it's missing or not a number.</summary>
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        string? value = user.FindFirst(OutpostClaimTypes.UserId)?.Value;
        if (int.TryParse(value, out int userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>The role name from the "role" claim, or null.</summary>
    public static string? GetRoleName(this ClaimsPrincipal user)
    {
        return user.FindFirst(OutpostClaimTypes.Role)?.Value;
    }
}
