using System.Text.Json.Serialization;
using AlmostNewOutpost.Api.Services;
using Microsoft.Extensions.Options;
using FrameworkCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;
using OutpostCorsOptions = AlmostNewOutpost.Api.Options.CorsOptions;

namespace AlmostNewOutpost.Api.Extensions;

/// <summary>
/// Registers controllers, JSON settings, ProblemDetails errors, CORS and the application services.
/// </summary>
public static class ApiServiceExtensions
{
    public static IServiceCollection AddOutpostApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Enums travel as text ("DustStorm"), not numbers.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Every error (including empty 401/403 responses) comes back as RFC 7807 ProblemDetails.
        services.AddProblemDetails();

        services.AddCors();
        services.AddOptions<FrameworkCorsOptions>()
            .Configure<IOptions<OutpostCorsOptions>>((cors, outpostCors) =>
            {
                cors.AddPolicy(OutpostCorsOptions.PolicyName, policy => policy
                    .WithOrigins(outpostCors.Value.AllowedOrigins)
                    .AllowAnyMethod()
                    .WithHeaders("Authorization", "Content-Type"));
            });

        services.AddScoped<ProductService>();
        services.AddScoped<PurchaseService>();

        return services;
    }
}
