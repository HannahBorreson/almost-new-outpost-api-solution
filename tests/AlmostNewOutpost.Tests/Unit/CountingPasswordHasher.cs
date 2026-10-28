using AlmostNewOutpost.Api.Models.Rows;
using Microsoft.AspNetCore.Identity;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// Wraps the real PasswordHasher and remembers which hashes it was asked to check,
/// so a test can prove the dummy hash is verified when the user doesn't exist.
/// </summary>
public class CountingPasswordHasher : IPasswordHasher<UserRow>
{
    private readonly PasswordHasher<UserRow> _inner = new PasswordHasher<UserRow>();

    public List<string> VerifiedHashes { get; } = new List<string>();

    public string HashPassword(UserRow user, string password)
    {
        return _inner.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyHashedPassword(UserRow user, string hashedPassword, string providedPassword)
    {
        VerifiedHashes.Add(hashedPassword);
        return _inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }
}
