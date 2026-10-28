using AlmostNewOutpost.Api.Extensions;
using AlmostNewOutpost.Api.Models.Requests;
using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Api.Controllers;

/// <summary>
/// Login, and "who am I?" for the signed-in user.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string InvalidLoginMessage = "Invalid username or password.";

    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Trade a username and password for a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        LoginResponse? response = await _authService.LoginAsync(request);
        if (response == null)
        {
            // Same message whether the username or the password was wrong.
            return Problem(title: InvalidLoginMessage, statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(response);
    }

    /// <summary>The signed-in user, from the id in their token.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me()
    {
        int? userId = User.GetUserId();
        UserDto? user = userId == null ? null : await _authService.GetUserAsync(userId.Value);
        if (user == null)
        {
            return Problem(title: "Your account could not be found. Please log in again.", statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(user);
    }
}
