using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// The almost_new_outpost_test database: built once per test run from the real SQL script,
/// and reset to the seed data before each API test.
/// </summary>
public class TestDatabase
{
    public const string AdminConnectionKey = "OUTPOST_TEST_ADMIN_CONNECTION";

    /// <summary>Must match &lt;UserSecretsId&gt; in AlmostNewOutpost.Tests.csproj.</summary>
    public const string UserSecretsId = "almost-new-outpost-tests-8d41a7c2";

    /// <summary>The app login and password created by the SQL script.</summary>
    public const string AppUser = "outpost_app";
    public const string AppPassword = "Outpost_Dev_2026!";

    public const string MissingConnectionMessage =
        "API tests skipped: set " + AdminConnectionKey + " to a MySQL root connection string, for example\n" +
        "  dotnet user-secrets set --project tests/AlmostNewOutpost.Tests \"" + AdminConnectionKey + "\" \"Server=localhost;Port=3306;User ID=root;Password=<your root password>;\"\n" +
        "or set the environment variable of the same name. See README.md.";

    private readonly string _adminConnectionString;
    private readonly List<string> _seedStatements;
    private readonly List<string> _allStatements;

    public TestDatabase(string adminConnectionString)
    {
        // The script contains 'outpost_app'@'localhost'. AllowUserVariables stops the MySQL
        // driver from mistaking "@'localhost'" for a query parameter.
        MySqlConnectionStringBuilder admin = new MySqlConnectionStringBuilder(adminConnectionString)
        {
            AllowUserVariables = true,
        };
        _adminConnectionString = admin.ConnectionString;

        string scriptPath = Path.Combine(AppContext.BaseDirectory, "database", "almost_new_outpost.sql");
        string testScript = SqlScriptRunner.ToTestScript(File.ReadAllText(scriptPath));
        SqlScriptRunner.EnsureOnlyTestDatabasesAreDropped(testScript);

        _allStatements = SqlScriptRunner.SplitStatements(testScript);
        _seedStatements = _allStatements
            .Where(statement => statement.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // The API under test connects as the limited app login, never as root.
        AppConnectionString = new MySqlConnectionStringBuilder(adminConnectionString)
        {
            UserID = AppUser,
            Password = AppPassword,
            Database = SqlScriptRunner.TestDatabaseName,
        }.ConnectionString;
    }

    /// <summary>The connection string the API uses during the tests (outpost_app on the test database).</summary>
    public string AppConnectionString { get; }

    /// <summary>
    /// The admin connection string from the environment variable or the test project's
    /// user-secrets, or null when neither is set.
    /// </summary>
    public static string? FindAdminConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets(UserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        string? value = configuration[AdminConnectionKey];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Drops and re-creates almost_new_outpost_test by running the whole script.</summary>
    public async Task BuildAsync()
    {
        await SqlScriptRunner.RunAsync(_adminConnectionString, _allStatements);
    }

    /// <summary>
    /// Puts the seed data back (removing anything a test added or changed) without
    /// dropping the database, so the API's open connections keep working.
    /// </summary>
    public async Task ResetDataAsync()
    {
        List<string> statements = new List<string>
        {
            "USE " + SqlScriptRunner.TestDatabaseName,
            "SET FOREIGN_KEY_CHECKS = 0",
            "TRUNCATE TABLE Purchases",
            "TRUNCATE TABLE Products",
            "TRUNCATE TABLE Users",
            "TRUNCATE TABLE Categories",
            "TRUNCATE TABLE Roles",
            "SET FOREIGN_KEY_CHECKS = 1",
        };
        statements.AddRange(_seedStatements);

        await SqlScriptRunner.RunAsync(_adminConnectionString, statements);
    }
}
