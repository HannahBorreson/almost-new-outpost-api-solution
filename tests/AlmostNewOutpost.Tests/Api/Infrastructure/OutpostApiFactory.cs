using AlmostNewOutpost.Api.Weather;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Starts the real API in memory for the tests: pointed at almost_new_outpost_test, with a
/// test signing key and a weather provider that is always Clear.
/// </summary>
public class OutpostApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The JWT signing key the API uses during the tests. Test-only.</summary>
    public const string SigningKey = "4106r24oTM5sY9nhKo7mRRdn1aBgpbMx0TyAjCWRkRf0B4QN61cyJivM7VHrsTNE";

    private readonly string _connectionString;

    public OutpostApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" skips user-secrets, Swagger and the startup database check.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Outpost"] = _connectionString,
                ["Jwt:SigningKey"] = SigningKey,
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWeatherProvider>();
            services.AddSingleton<IWeatherProvider, FixedWeatherProvider>();
        });
    }

    /// <summary>An HTTP client for the in-memory API, using https like the real one.</summary>
    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }
}
