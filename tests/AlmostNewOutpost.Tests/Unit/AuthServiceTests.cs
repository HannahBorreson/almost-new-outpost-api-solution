using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Models.Rows;
using AlmostNewOutpost.Api.Repositories;
using AlmostNewOutpost.Api.Services;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace AlmostNewOutpost.Tests.Unit;

/// <summary>
/// Login rules, tested with an NSubstitute user repository instead of the database.
/// </summary>
public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly PasswordHasher<UserRow> _hasher;
    private readonly AuthService _service;
    private readonly string _medicHash;

    public AuthServiceTests()
    {
        // A partial substitute: the real hashing still runs, but NSubstitute records every call.
        // ForPartsOf can't fill in optional constructor arguments, so the default options are passed in.
        _hasher = Substitute.ForPartsOf<PasswordHasher<UserRow>>(TestSettings.Wrap(new PasswordHasherOptions()));

        UserRow medic = new UserRow { UserId = 3, Username = "medic", RoleId = 3, RoleName = "Medic" };
        _medicHash = new PasswordHasher<UserRow>().HashPassword(medic, "medic123");
        medic.PasswordHash = _medicHash;

        // Any username finds nobody, except "medic".
        _users.GetByUsernameAsync(Arg.Any<string>()).Returns((UserRow?)null);
        _users.GetByUsernameAsync("medic").Returns(medic);

        TokenService tokens = new TokenService(TestSettings.Wrap(TestSettings.Jwt()), TimeProvider.System);
        _service = new AuthService(_users, _hasher, tokens);
    }

    [Fact]
    public async Task CorrectPassword_ReturnsTokenAndUser()
    {
        // Arrange
        LoginRequest request = new LoginRequest { Username = "medic", Password = "medic123" };

        // Act
        LoginResponse? response = await _service.LoginAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.False(string.IsNullOrEmpty(response.AccessToken));
        Assert.Equal(3, response.User.Id);
        Assert.Equal("medic", response.User.Username);
        Assert.Equal("Medic", response.User.Role);
    }

    [Fact]
    public async Task UsernameWithSpaces_IsTrimmed()
    {
        // Arrange
        LoginRequest request = new LoginRequest { Username = "  medic ", Password = "medic123" };

        // Act
        LoginResponse? response = await _service.LoginAsync(request);

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task WrongPassword_Fails()
    {
        // Arrange
        LoginRequest request = new LoginRequest { Username = "medic", Password = "wrong" };

        // Act
        LoginResponse? response = await _service.LoginAsync(request);

        // Assert
        Assert.Null(response);
    }

    [Fact]
    public async Task UnknownUser_Fails_AndStillVerifiesTheDummyHash()
    {
        // Arrange
        LoginRequest request = new LoginRequest { Username = "nobody", Password = "medic123" };

        // Act
        LoginResponse? response = await _service.LoginAsync(request);

        // Assert
        Assert.Null(response);
        // Exactly one hash was verified...
        _hasher.Received(1).VerifyHashedPassword(Arg.Any<UserRow>(), Arg.Any<string>(), Arg.Any<string>());
        // ...and it was a real v3, 100,000-iteration hash that is not the medic's.
        _hasher.Received(1).VerifyHashedPassword(
            Arg.Any<UserRow>(),
            Arg.Is<string>(hash => hash != _medicHash && hash.StartsWith("AQAAAAIAAYag", StringComparison.Ordinal)),
            Arg.Any<string>());
    }

    [Theory]
    [InlineData("", "medic123")]
    [InlineData("   ", "medic123")]
    [InlineData("medic", "")]
    public async Task BlankInput_Fails(string username, string password)
    {
        // Arrange
        LoginRequest request = new LoginRequest { Username = username, Password = password };

        // Act
        LoginResponse? response = await _service.LoginAsync(request);

        // Assert
        Assert.Null(response);
    }
}
