using AlmostNewOutpost.Api.Data;
using AlmostNewOutpost.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Api.Controllers.Admin;

/// <summary>
/// Lets an admin read user accounts. It reads them straight from the database with EF Core.
/// </summary>
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly OutpostDbContext _db;

    public AdminUsersController(OutpostDbContext db)
    {
        _db = db;
    }

    /// <summary>Every active user sorted by username, or every user when includeArchived is true.</summary>
    [HttpGet]
    public async Task<ActionResult<List<AdminUserDto>>> GetAll([FromQuery] bool includeArchived = false)
    {
        List<AdminUserDto> users = await (
            from user in _db.Users
            join role in _db.Roles on user.RoleId equals role.RoleId
            where includeArchived || user.ArchivedAtUtc == null
            orderby user.Username
            select new AdminUserDto
            {
                Id = user.UserId,
                Username = user.Username,
                RoleId = role.RoleId,
                Role = role.RoleName,
                ArchivedAtUtc = user.ArchivedAtUtc,
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>One user by id, archived or not.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminUserDto>> GetById(int id)
    {
        // Not named "user": the query below already uses that name, and C# won't allow both.
        AdminUserDto? adminUser = await (
            from user in _db.Users
            join role in _db.Roles on user.RoleId equals role.RoleId
            where user.UserId == id
            select new AdminUserDto
            {
                Id = user.UserId,
                Username = user.Username,
                RoleId = role.RoleId,
                Role = role.RoleName,
                ArchivedAtUtc = user.ArchivedAtUtc,
            })
            .FirstOrDefaultAsync();

        if (adminUser == null)
        {
            return Problem(title: "User not found.", statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(adminUser);
    }
}
