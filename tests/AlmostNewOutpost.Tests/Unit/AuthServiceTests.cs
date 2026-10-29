using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Services;
using Microsoft.AspNetCore.Identity;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// Login rules, tested with a fake user repository instead of the database.
/// </summary>
public class AuthServiceTests
{
    private readonly FakeUserRepository _users = new FakeUserRepository();
    private readonly CountingPasswordHasher _hasher = new CountingPasswordHasher();
    private readonly AuthService _service;
    private readonly string _medicHash;

    public AuthServiceTests()
    {
        UserRow medic = new UserRow { UserId = 3, Username = "medic", RoleId = 3, RoleName = "Medic" };
        _medicHash = new PasswordHasher<UserRow>().HashPassword(medic, "medic123");
        medic.PasswordHash = _medicHash;
        _users.Add(medic);

        TokenService tokens = new TokenService(TestSettings.Wrap(TestSettings.Jwt()), TimeProvider.System);
        _service = new AuthService(_users, _hasher, tokens);
    }

    [Fact]
    public async Task CorrectPassword_ReturnsTokenAndUser()
    {
        LoginResponse? response = await _service.LoginAsync(new LoginRequest { Username = "medic", Password = "medic123" });

        Assert.NotNull(response);
        Assert.False(string.IsNullOrEmpty(response.AccessToken));
        Assert.Equal(3, response.User.Id);
        Assert.Equal("medic", response.User.Username);
        Assert.Equal("Medic", response.User.Role);
    }

    [Fact]
    public async Task UsernameWithSpaces_IsTrimmed()
    {
        LoginResponse? response = await _service.LoginAsync(new LoginRequest { Username = "  medic ", Password = "medic123" });

        Assert.NotNull(response);
    }

    [Fact]
    public async Task WrongPassword_Fails()
    {
        LoginResponse? response = await _service.LoginAsync(new LoginRequest { Username = "medic", Password = "wrong" });

        Assert.Null(response);
    }

    [Fact]
    public async Task UnknownUser_Fails_AndStillVerifiesTheDummyHash()
    {
        LoginResponse? response = await _service.LoginAsync(new LoginRequest { Username = "nobody", Password = "medic123" });

        Assert.Null(response);
        string verified = Assert.Single(_hasher.VerifiedHashes);
        Assert.NotEqual(_medicHash, verified);
        Assert.StartsWith("AQAAAAIAAYag", verified); // a real v3, 100,000-iteration hash
    }

    [Theory]
    [InlineData("", "medic123")]
    [InlineData("   ", "medic123")]
    [InlineData("medic", "")]
    public async Task BlankInput_Fails(string username, string password)
    {
        LoginResponse? response = await _service.LoginAsync(new LoginRequest { Username = username, Password = password });

        Assert.Null(response);
    }
}
