using AlmostNewOutpost.Api.Options;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// Settings objects shared by the unit tests (the same values as appsettings.json).
/// </summary>
public static class TestSettings
{
    /// <summary>A test-only signing key. Never use it outside the tests.</summary>
    public const string SigningKey = "4106r24oTM5sY9nhKo7mRRdn1aBgpbMx0TyAjCWRkRf0B4QN61cyJivM7VHrsTNE";

    public static JwtOptions Jwt()
    {
        return new JwtOptions
        {
            Issuer = "almost-new-outpost-api",
            Audience = "almost-new-outpost-ui",
            ExpiryMinutes = 60,
            SigningKey = SigningKey,
        };
    }

    public static Microsoft.Extensions.Options.IOptions<T> Wrap<T>(T value)
        where T : class
    {
        return Microsoft.Extensions.Options.Options.Create(value);
    }
}
