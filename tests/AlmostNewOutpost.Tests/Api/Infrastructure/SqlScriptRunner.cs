using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Runs database/almost_new_outpost.sql the way MySQL Workbench would, but against the test
/// database: it renames almost_new_outpost to almost_new_outpost_test, handles DELIMITER lines
/// itself (they're a client command, not SQL), and runs the statements one at a time.
/// </summary>
public static class SqlScriptRunner
{
    public const string DevDatabaseName = "almost_new_outpost";
    public const string TestDatabaseName = "almost_new_outpost_test";

    /// <summary>Points every reference to the dev database at the test database instead.</summary>
    public static string ToTestScript(string script)
    {
        // \b stops this from matching inside names that are already almost_new_outpost_test.
        return Regex.Replace(script, @"\b" + DevDatabaseName + @"\b", TestDatabaseName);
    }

    /// <summary>
    /// Safety check: refuse to run a script that could drop a database whose name doesn't
    /// end in _test (for example, your real dev database).
    /// </summary>
    public static void EnsureOnlyTestDatabasesAreDropped(string script)
    {
        MatchCollection drops = Regex.Matches(
            script,
            @"DROP\s+DATABASE\s+(?:IF\s+EXISTS\s+)?`?(\w+)`?",
            RegexOptions.IgnoreCase);

        foreach (Match drop in drops)
        {
            string databaseName = drop.Groups[1].Value;
            if (!databaseName.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Refusing to run the SQL script: it would drop '{databaseName}', which is not a _test database.");
            }
        }
    }

    /// <summary>
    /// Splits the script into single statements. Comments are removed, and DELIMITER lines
    /// change what marks the end of a statement (";" normally, "$$" around procedures).
    /// </summary>
    public static List<string> SplitStatements(string script)
    {
        List<string> statements = new List<string>();
        StringBuilder current = new StringBuilder();
        string delimiter = ";";

        foreach (string rawLine in script.Split('\n'))
        {
            string line = RemoveComment(rawLine.TrimEnd('\r'));
            string trimmed = line.Trim();

            if (current.Length == 0 && trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
            {
                delimiter = trimmed.Substring("DELIMITER ".Length).Trim();
                continue;
            }

            current.AppendLine(line);

            if (trimmed.EndsWith(delimiter, StringComparison.Ordinal))
            {
                string statement = current.ToString().Trim();
                statement = statement.Substring(0, statement.Length - delimiter.Length).Trim();
                if (statement.Length > 0)
                {
                    statements.Add(statement);
                }

                current.Clear();
            }
        }

        if (current.ToString().Trim().Length > 0)
        {
            throw new InvalidOperationException("The SQL script ends with a statement that has no delimiter.");
        }

        return statements;
    }

    /// <summary>Runs each statement in order on one connection.</summary>
    public static async Task RunAsync(string connectionString, IEnumerable<string> statements)
    {
        await using MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (string statement in statements)
        {
            await using MySqlCommand command = new MySqlCommand(statement, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Removes a "-- comment" from the end of a line, ignoring "--" that appears inside quotes.
    /// </summary>
    private static string RemoveComment(string line)
    {
        char? openQuote = null;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (openQuote != null)
            {
                if (c == '\\')
                {
                    i++; // skip the escaped character
                }
                else if (c == openQuote)
                {
                    openQuote = null;
                }

                continue;
            }

            if (c == '\'' || c == '"' || c == '`')
            {
                openQuote = c;
            }
            else if (c == '-' && i + 1 < line.Length && line[i + 1] == '-' &&
                     (i + 2 == line.Length || char.IsWhiteSpace(line[i + 2])))
            {
                return line.Substring(0, i);
            }
        }

        return line;
    }
}
