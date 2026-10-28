namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Shared by every API test: builds the test database once per run and starts one copy
/// of the API. If no admin connection string is set, it does nothing (the tests are skipped).
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    public TestDatabase? Database { get; private set; }

    public OutpostApiFactory? Factory { get; private set; }

    public async ValueTask InitializeAsync()
    {
        string? adminConnectionString = TestDatabase.FindAdminConnectionString();
        if (adminConnectionString == null)
        {
            return;
        }

        Database = new TestDatabase(adminConnectionString);
        await Database.BuildAsync();
        Factory = new OutpostApiFactory(Database.AppConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
