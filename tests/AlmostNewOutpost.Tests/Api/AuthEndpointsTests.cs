using System.Net;
using System.Net.Http.Json;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Tests.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Tests.Api;

/// <summary>
/// POST /api/auth/login, GET /api/auth/me, and what happens with missing or bad tokens.
/// </summary>
[Collection(ApiCollection.Name)]
public class AuthEndpointsTests : ApiTestBase
{
    public AuthEndpointsTests(ApiFixture fixture)
        : base(fixture)
    {
    }

    [ApiTheory]
    [InlineData("citizen", "Citizen")]
    [InlineData("scout", "Scout")]
    [InlineData("medic", "Medic")]
    [InlineData("security", "Security")]
    [InlineData("admin", "MerchantAdmin")]
    public async Task Login_SeededUser_ReturnsToken(string username, string role)
    {
        HttpResponseMessage response = await AnonymousClient().PostAsJsonAsync(
            "/api/auth/login", new { username, password = username + "123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LoginResponse login = await ReadAsync<LoginResponse>(response);
        Assert.False(string.IsNullOrEmpty(login.AccessToken));
        Assert.Equal(username, login.User.Username);
        Assert.Equal(role, login.User.Role);
        Assert.InRange(login.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(58), DateTime.UtcNow.AddMinutes(61));
    }

    [ApiFact]
    public async Task Login_WrongPassword_Returns401WithGenericMessage()
    {
        HttpResponseMessage response = await AnonymousClient().PostAsJsonAsync(
            "/api/auth/login", new { username = "medic", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        ProblemDetails problem = await ReadAsync<ProblemDetails>(response);
        Assert.Equal("Invalid username or password.", problem.Title);
    }

    [ApiFact]
    public async Task Login_UnknownUser_ReturnsTheSame401()
    {
        HttpResponseMessage response = await AnonymousClient().PostAsJsonAsync(
            "/api/auth/login", new { username = "nobody", password = "medic123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        ProblemDetails problem = await ReadAsync<ProblemDetails>(response);
        Assert.Equal("Invalid username or password.", problem.Title);
    }

    [ApiFact]
    public async Task Login_BlankFields_Returns400WithFieldErrors()
    {
        HttpResponseMessage response = await AnonymousClient().PostAsJsonAsync(
            "/api/auth/login", new { username = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains("Username", problem.Errors.Keys);
        Assert.Contains("Password", problem.Errors.Keys);
    }

    [ApiFact]
    public async Task Login_UsernameTooLong_Returns400()
    {
        HttpResponseMessage response = await AnonymousClient().PostAsJsonAsync(
            "/api/auth/login", new { username = new string('u', 51), password = "secret" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [ApiFact]
    public async Task Me_ReturnsTheUserFromTheToken()
    {
        HttpClient client = await ClientForAsync("medic");

        HttpResponseMessage response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserDto user = await ReadAsync<UserDto>(response);
        Assert.Equal(3, user.Id);
        Assert.Equal("medic", user.Username);
        Assert.Equal("Medic", user.Role);
    }

    [ApiFact]
    public async Task NoToken_Returns401ProblemDetails()
    {
        HttpResponseMessage response = await AnonymousClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        ProblemDetails problem = await ReadAsync<ProblemDetails>(response);
        Assert.Equal(401, problem.Status);
        Assert.False(string.IsNullOrEmpty(problem.Title));
    }

    [ApiFact]
    public async Task ExpiredToken_Returns401()
    {
        HttpResponseMessage response = await ClientWithToken(TokenHelper.ExpiredMedicToken()).GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [ApiFact]
    public async Task TokenSignedWithWrongKey_Returns401()
    {
        HttpResponseMessage response = await ClientWithToken(TokenHelper.WrongKeyAdminToken()).GetAsync("/api/admin/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [ApiFact]
    public async Task Health_IsAnonymous_AndReportsTheDatabase()
    {
        HttpResponseMessage response = await AnonymousClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        HealthDto health = await ReadAsync<HealthDto>(response);
        Assert.Equal("ok", health.Status);
        Assert.Equal("ok", health.Database);
    }
}
