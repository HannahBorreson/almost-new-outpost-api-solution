using System.Text.RegularExpressions;
using AlmostNewOutpost.Api.Models.Rows;
using Microsoft.AspNetCore.Identity;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// Checks that every password hash seeded by database/almost_new_outpost.sql really matches
/// username + "123". If this fails, nobody can log in.
/// </summary>
public class SeedHashTests
{
    public static TheoryData<string> SeededUsers => new TheoryData<string> { "citizen", "scout", "medic", "security", "admin" };

    [Theory]
    [MemberData(nameof(SeededUsers))]
    public void SeededHash_VerifiesAgainstUsernamePlus123(string username)
    {
        // Arrange
        string hash = FindSeededHash(username);
        PasswordHasher<UserRow> hasher = new PasswordHasher<UserRow>();

        // Act
        PasswordVerificationResult result = hasher.VerifyHashedPassword(new UserRow(), hash, username + "123");

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    private static string FindSeededHash(string username)
    {
        string script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "database", "almost_new_outpost.sql"));

        // Matches a line like:  (3, 'medic',    'AQAAAAIAAYag...==', 3),
        Match match = Regex.Match(script, @"\(\s*\d+\s*,\s*'" + Regex.Escape(username) + @"'\s*,\s*'(AQAAAA[^']+)'");
        Assert.True(match.Success, $"No seeded hash found for {username}.");
        return match.Groups[1].Value;
    }
}
