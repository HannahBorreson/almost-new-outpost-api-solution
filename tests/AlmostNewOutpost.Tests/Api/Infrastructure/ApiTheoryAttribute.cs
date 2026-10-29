using System.Runtime.CompilerServices;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Use instead of [Theory] for tests that need MySQL. Skipped (with instructions) when
/// OUTPOST_TEST_ADMIN_CONNECTION isn't set.
/// </summary>
public class ApiTheoryAttribute : TheoryAttribute
{
    public ApiTheoryAttribute(
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
