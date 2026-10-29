using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Models.Rows;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Api.Repositories;

/// <summary>
/// Calls the user stored procedures. The {values} in the interpolated SQL become
/// DbParameters, so user input is never pasted into the SQL text.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly OutpostDbContext _db;

    public UserRepository(OutpostDbContext db)
    {
        _db = db;
    }

    public async Task<UserRow?> GetByUsernameAsync(string username)
    {
        // A CALL can't be wrapped in a subquery, so read the rows first (ToListAsync)
        // and only then pick one with ordinary LINQ-to-objects.
        List<UserRow> rows = await _db.Database
            .SqlQuery<UserRow>($"CALL sp_GetUserByUsername({username})")
            .ToListAsync();

        return rows.FirstOrDefault();
    }

    public async Task<UserSummaryRow?> GetByIdAsync(int userId)
    {
        List<UserSummaryRow> rows = await _db.Database
            .SqlQuery<UserSummaryRow>($"CALL sp_GetUserById({userId})")
            .ToListAsync();

        return rows.FirstOrDefault();
    }
}
