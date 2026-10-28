using AlmostNewOutpost.Api.Models.Responses;
using AlmostNewOutpost.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmostNewOutpost.Api.Controllers;

/// <summary>
/// Is the API up, and can it reach the database? No login needed.
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly ICategoryRepository _categories;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ICategoryRepository categories, ILogger<HealthController> logger)
    {
        _categories = categories;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<HealthDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthDto>> Get()
    {
        HealthDto health = new HealthDto();
        try
        {
            await _categories.GetAllAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Health check could not reach the database: {Reason}", exception.Message);
            health.Database = "unreachable";
        }

        return Ok(health);
    }
}
