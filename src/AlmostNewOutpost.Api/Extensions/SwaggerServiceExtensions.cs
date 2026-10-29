using Microsoft.OpenApi;

namespace AlmostNewOutpost.Api.Extensions;

/// <summary>
/// Swagger (OpenAPI) docs with an Authorize button, so you can paste a token and try every endpoint.
/// The UI is only switched on in Development (see ApplicationBuilderExtensions).
/// </summary>
public static class SwaggerServiceExtensions
{
    private const string SchemeName = "Bearer";

    public static IServiceCollection AddOutpostSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Almost New 24/7 Outpost API",
                Version = "v1",
                Description = "Mostly New Goods Since The Collapse. Log in with POST /api/auth/login, then click Authorize and paste the accessToken.",
            });

            options.AddSecurityDefinition(SchemeName, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the accessToken from /api/auth/login (without the word Bearer).",
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, document)] = new List<string>(),
            });
        });

        return services;
    }
}
