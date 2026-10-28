using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Options;
using AlmostNewOutpost.Api.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Api.Extensions;

/// <summary>
/// Registers the EF Core context (Oracle's MySQL provider) and the repositories.
/// </summary>
public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddOutpostDatabase(this IServiceCollection services)
    {
        // The connection string is read when the context is first needed, not at startup,
        // so tests can swap in their own value.
        services.AddDbContext<OutpostDbContext>((serviceProvider, options) =>
        {
            string connectionString = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value.Outpost;
            options.UseMySQL(connectionString);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();

        return services;
    }
}
