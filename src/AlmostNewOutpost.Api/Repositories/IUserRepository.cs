using AlmostNewOutpost.Api.Models.Rows;

namespace AlmostNewOutpost.Api.Repositories;

/// <summary>
/// Reads users through the user stored procedures.
/// </summary>
public interface IUserRepository
{
    /// <summary>sp_GetUserByUsername. Includes the password hash. Null if no such user.</summary>
    Task<UserRow?> GetByUsernameAsync(string username);

    /// <summary>sp_GetUserById. No password hash. Null if no such user.</summary>
    Task<UserSummaryRow?> GetByIdAsync(int userId);
}
