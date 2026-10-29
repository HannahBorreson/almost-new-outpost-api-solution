using System.Text;
using AlmostNewOutpost.Api.Authorization;
using AlmostNewOutpost.Api.Models.Constants;
using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Options;
using AlmostNewOutpost.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AlmostNewOutpost.Api.Extensions;

/// <summary>
/// Sets up login, JWT checking and the authorization policies.
/// </summary>
public static class AuthServiceExtensions
{
    public static IServiceCollection AddOutpostAuth(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher<UserRow>, PasswordHasher<UserRow>>();
        services.AddSingleton<TokenService>();
        services.AddScoped<AuthService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through options so the signing key is read from configuration when
        // it's first needed (user-secrets in development, test values in the tests).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                JwtOptions jwt = jwtOptions.Value;

                // Keep claim names exactly as we issued them ("sub", "role"), instead of
                // letting ASP.NET rename them to long XML-style claim types.
                bearer.MapInboundClaims = false;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = OutpostClaimTypes.Username,
                    RoleClaimType = OutpostClaimTypes.Role,
                };
            });

        services.AddAuthorization(options =>
        {
            // Secure by default: every endpoint needs a signed-in user unless it says [AllowAnonymous].
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole(RoleNames.MerchantAdmin));

            options.AddPolicy(PolicyNames.CanPurchaseProduct, policy =>
                policy.Requirements.Add(new CanPurchaseProductRequirement()));
        });

        services.AddSingleton<IAuthorizationHandler, CanPurchaseProductHandler>();

        return services;
    }
}
