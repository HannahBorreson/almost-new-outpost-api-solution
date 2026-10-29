using System.Text;
using AlmostNewOutpost.Api.Services;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// The JWT carries the right claims, lasts 60 minutes, and validates with our settings.
/// </summary>
public class TokenServiceTests
{
    private static TokenService CreateService(TimeProvider clock)
    {
        return new TokenService(TestSettings.Wrap(TestSettings.Jwt()), clock);
    }

    [Fact]
    public void Token_ContainsTheExpectedClaims()
    {
        // Arrange
        TokenService service = CreateService(TimeProvider.System);

        // Act
        AccessToken token = service.CreateToken(3, "medic", "Medic");
        JsonWebToken jwt = new JsonWebToken(token.Token);

        // Assert
        Assert.Equal("3", jwt.GetClaim("sub").Value);
        Assert.Equal("medic", jwt.GetClaim("unique_name").Value);
        Assert.Equal("Medic", jwt.GetClaim("role").Value);
        Assert.True(Guid.TryParse(jwt.GetClaim("jti").Value, out _));
        Assert.Equal("almost-new-outpost-api", jwt.Issuer);
        Assert.Contains("almost-new-outpost-ui", jwt.Audiences);
    }

    [Fact]
    public void Token_ExpiresAfter60Minutes()
    {
        // Arrange
        DateTimeOffset now = new DateTimeOffset(2026, 10, 27, 21, 0, 0, TimeSpan.Zero);
        TokenService service = CreateService(new FakeTimeProvider(now));
        DateTime expected = now.UtcDateTime.AddMinutes(60);

        // Act
        AccessToken token = service.CreateToken(3, "medic", "Medic");
        JsonWebToken jwt = new JsonWebToken(token.Token);

        // Assert
        Assert.Equal(expected, token.ExpiresAtUtc);
        Assert.Equal(expected, jwt.ValidTo);
        Assert.Equal(DateTimeKind.Utc, token.ExpiresAtUtc.Kind);
    }

    [Fact]
    public async Task Token_ValidatesWithTheConfiguredParameters()
    {
        // Arrange
        TokenService service = CreateService(TimeProvider.System);
        AccessToken token = service.CreateToken(5, "admin", "MerchantAdmin");

        // Act
        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, ValidationParameters(TestSettings.SigningKey));

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Token_SignedWithAnotherKey_DoesNotValidate()
    {
        // Arrange
        TokenService service = CreateService(TimeProvider.System);
        AccessToken token = service.CreateToken(5, "admin", "MerchantAdmin");
        string otherKey = new string('x', 64);

        // Act
        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, ValidationParameters(otherKey));

        // Assert
        Assert.False(result.IsValid);
    }

    private static TokenValidationParameters ValidationParameters(string signingKey)
    {
        return new TokenValidationParameters
        {
            ValidIssuer = "almost-new-outpost-api",
            ValidAudience = "almost-new-outpost-ui",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }
}
