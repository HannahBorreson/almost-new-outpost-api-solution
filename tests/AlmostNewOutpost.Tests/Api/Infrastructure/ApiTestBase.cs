using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Base class for API tests. Before every test the seed data is restored and the weather
/// override is cleared, so tests never depend on each other. Each test class must also be
/// marked [Collection(ApiCollection.Name)] so it shares the one ApiFixture.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    protected ApiTestBase(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    protected ApiFixture Fixture { get; }

    protected OutpostApiFactory Factory =>
        Fixture.Factory ?? throw new InvalidOperationException(TestDatabase.MissingConnectionMessage);

    public async ValueTask InitializeAsync()
    {
        if (Fixture.Database == null)
        {
            throw new InvalidOperationException(TestDatabase.MissingConnectionMessage);
        }

        await Fixture.Database.ResetDataAsync();
        Factory.Services.GetRequiredService<WeatherState>().ClearOverride();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>A client with no token.</summary>
    protected HttpClient AnonymousClient()
    {
        return Factory.CreateHttpsClient();
    }

    /// <summary>A client for a token you made yourself (see TokenHelper).</summary>
    protected HttpClient ClientWithToken(string token)
    {
        HttpClient client = Factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Logs in through the real endpoint as a seeded user (password = username + "123").</summary>
    protected async Task<HttpClient> ClientForAsync(string username)
    {
        HttpClient client = Factory.CreateHttpsClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = username + "123" });
        response.EnsureSuccessStatusCode();

        LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJson.Options);
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    /// <summary>Runs some code with the API's own database context, to arrange or check data.</summary>
    protected async Task<T> WithDatabaseAsync<T>(Func<OutpostDbContext, Task<T>> action)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        OutpostDbContext db = scope.ServiceProvider.GetRequiredService<OutpostDbContext>();
        return await action(db);
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        T? value = await response.Content.ReadFromJsonAsync<T>(TestJson.Options);
        Assert.NotNull(value);
        return value;
    }
}
