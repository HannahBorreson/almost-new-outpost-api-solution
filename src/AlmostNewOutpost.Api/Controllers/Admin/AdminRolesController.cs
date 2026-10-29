using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Api.Controllers.Admin;

/// <summary>
/// Lists the roles a user can have. It reads them straight from the database with EF Core.
/// </summary>
[ApiController]
[Route("api/admin/roles")]
public class AdminRolesController : ControllerBase
{
    private readonly OutpostDbContext _db;

    public AdminRolesController(OutpostDbContext db)
    {
        _db = db;
    }

    /// <summary>Every role, sorted by id.</summary>
    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAll()
    {
        List<RoleDto> roles = await _db.Roles
            .OrderBy(role => role.RoleId)
            .Select(role => new RoleDto { RoleId = role.RoleId, RoleName = role.RoleName })
            .ToListAsync();

        return Ok(roles);
    }
}
