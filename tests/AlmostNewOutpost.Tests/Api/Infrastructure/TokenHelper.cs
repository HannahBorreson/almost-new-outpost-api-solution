using System.Text;
using AlmostNewOutpost.Api.Models.Constants;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Makes JWTs by hand for the security tests: expired tokens and tokens signed with the wrong key.
/// </summary>
public static class TokenHelper
{
    public static string CreateToken(int userId, string username, string role, string signingKey, DateTime expiresUtc)
    {
        SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
        {
            Issuer = "almost-new-outpost-api",
            Audience = "almost-new-outpost-ui",
            IssuedAt = expiresUtc.AddMinutes(-60),
            NotBefore = expiresUtc.AddMinutes(-60),
            Expires = expiresUtc,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [OutpostClaimTypes.UserId] = userId.ToString(),
                [OutpostClaimTypes.Username] = username,
                [OutpostClaimTypes.Role] = role,
                [OutpostClaimTypes.TokenId] = Guid.NewGuid().ToString(),
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>A Medic token that expired five minutes ago (well past the 30-second clock skew).</summary>
    public static string ExpiredMedicToken()
    {
        return CreateToken(3, "medic", RoleNames.Medic, OutpostApiFactory.SigningKey, DateTime.UtcNow.AddMinutes(-5));
    }

    /// <summary>An admin token that is valid in every way except it was signed with a different key.</summary>
    public static string WrongKeyAdminToken()
    {
        string otherKey = "ThisIsNotTheOutpostSigningKey_ThisIsNotTheOutpostSigningKey_1234";
        return CreateToken(5, "admin", RoleNames.MerchantAdmin, otherKey, DateTime.UtcNow.AddMinutes(30));
    }
}
