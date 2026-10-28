using System.Runtime.CompilerServices;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Use instead of [Fact] for tests that need MySQL. When OUTPOST_TEST_ADMIN_CONNECTION isn't
/// set, the test is skipped with a message saying how to set it, instead of failing.
/// </summary>
public class ApiFactAttribute : FactAttribute
{
    public ApiFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (TestDatabase.FindAdminConnectionString() == null)
        {
            Skip = TestDatabase.MissingConnectionMessage;
        }
    }
}
