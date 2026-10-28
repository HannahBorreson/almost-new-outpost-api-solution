using System.Text;
using AlmostNewOutpost.Api.Options;

namespace AlmostNewOutpost.Api.Extensions;

/// <summary>
/// Binds the settings classes to configuration and checks them when the app starts,
/// so a missing secret fails fast with a readable message instead of a strange error later.
/// </summary>
public static class OptionsServiceExtensions
{
    public static IServiceCollection AddOutpostOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                jwt => string.IsNullOrEmpty(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) >= JwtOptions.MinimumKeyBytes,
                $"Jwt:SigningKey must be at least {JwtOptions.MinimumKeyBytes} bytes long. Generate a longer key (see README).")
            .ValidateOnStart();

        services.AddOptions<WeatherOptions>()
            .Bind(configuration.GetSection(WeatherOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(weather => weather.Weights.Total > 0, "Weather:Weights must add up to more than 0.")
            .Validate(weather => weather.Provider == "Random", "Weather:Provider must be \"Random\" (the only provider until the weather lab).")
            .ValidateOnStart();

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
