using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Repositories;
using Microsoft.AspNetCore.Identity;

namespace AlmostNewOutpost.Api.Services;

/// <summary>
/// Logs users in: looks the user up, checks the password against the stored hash, and issues
/// a token. Every kind of failure looks the same to the caller, so an attacker learns nothing.
/// </summary>
public class AuthService
{
    // A real PasswordHasher v3 hash (PBKDF2-HMAC-SHA512, 100,000 iterations) of a random
    // password nobody knows. When the username doesn't exist we still check the password
    // against this, so a wrong username takes as long as a wrong password and response
    // times don't reveal which usernames exist.
    private const string DummyHash =
        "AQAAAAIAAYagAAAAECVO9P0N0wmtiXlg1TBfJQ1nUJElAQnVLnyrr2xVZ+BgooIa3vDAc7TJiWkqauMKzQ==";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher<UserRow> _passwordHasher;
    private readonly TokenService _tokenService;

    public AuthService(IUserRepository users, IPasswordHasher<UserRow> passwordHasher, TokenService tokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Returns the login response, or null when the username or password is wrong.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        string username = (request.Username ?? string.Empty).Trim();
        string password = request.Password ?? string.Empty;
        if (username.Length == 0 || password.Length == 0)
        {
            return null;
        }

        UserRow? user = await _users.GetByUsernameAsync(username);
        if (user == null)
        {
            _passwordHasher.VerifyHashedPassword(new UserRow(), DummyHash, password);
            return null;
        }

        PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        // SuccessRehashNeeded still means the password is right. We don't rehash, because
        // the app's database login has no procedure for updating users.
        bool passwordIsCorrect =
            result == PasswordVerificationResult.Success ||
            result == PasswordVerificationResult.SuccessRehashNeeded;
        if (!passwordIsCorrect)
        {
            return null;
        }

        AccessToken token = _tokenService.CreateToken(user.UserId, user.Username, user.RoleName);
        return new LoginResponse
        {
            AccessToken = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = UserDto.FromRow(user),
        };
    }

    /// <summary>
    /// The signed-in user, looked up by the id in their token. Null if they no longer exist.
    /// </summary>
    public async Task<UserDto?> GetUserAsync(int userId)
    {
        UserSummaryRow? user = await _users.GetByIdAsync(userId);
        return user == null ? null : UserDto.FromRow(user);
    }
}
