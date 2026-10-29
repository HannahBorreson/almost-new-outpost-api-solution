using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Repositories;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// An in-memory stand-in for the database, so AuthService can be tested without MySQL.
/// </summary>
public class FakeUserRepository : IUserRepository
{
    private readonly List<UserRow> _users = new List<UserRow>();

    public void Add(UserRow user)
    {
        _users.Add(user);
    }

    public Task<UserRow?> GetByUsernameAsync(string username)
    {
        UserRow? user = _users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<UserSummaryRow?> GetByIdAsync(int userId)
    {
        UserRow? user = _users.FirstOrDefault(u => u.UserId == userId);
        UserSummaryRow? summary = user == null
            ? null
            : new UserSummaryRow { UserId = user.UserId, Username = user.Username, RoleId = user.RoleId, RoleName = user.RoleName };
        return Task.FromResult(summary);
    }
}
