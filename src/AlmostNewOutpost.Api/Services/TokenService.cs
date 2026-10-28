using System.Text;
using AlmostNewOutpost.Api.Models.Constants;
using AlmostNewOutpost.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// Builds the signed JWT a user receives at login. The token carries sub (user id),
/// unique_name (username), role and jti (a unique token id), signed with HS256.
/// </summary>
public class TokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public AccessToken CreateToken(int userId, string username, string roleName)
    {
        // JWT times are whole seconds, so drop the milliseconds to keep "exp" and
        // expiresAtUtc exactly the same.
        DateTime now = TruncateToSeconds(_timeProvider.GetUtcNow().UtcDateTime);
        DateTime expires = now.AddMinutes(_options.ExpiryMinutes);

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [OutpostClaimTypes.UserId] = userId.ToString(),
                [OutpostClaimTypes.Username] = username,
                [OutpostClaimTypes.Role] = roleName,
                [OutpostClaimTypes.TokenId] = Guid.NewGuid().ToString(),
            },
        };

        JsonWebTokenHandler handler = new JsonWebTokenHandler();
        string token = handler.CreateToken(descriptor);

        return new AccessToken { Token = token, ExpiresAtUtc = expires };
    }

    private static DateTime TruncateToSeconds(DateTime value)
    {
        return new DateTime(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }
}
