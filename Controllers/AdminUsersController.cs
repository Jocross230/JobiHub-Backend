using CVBuilder.API.Data;
using CVBuilder.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/Admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public AdminUsersController(CvBuilderDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    // ==========================================
    // GET ALL USERS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                fullName = x.FullName,
                email = x.Email,
                role = x.Role,
                status = x.IsActive ? "active" : "inactive",
                createdAt = x.CreatedAt,
                cvCount = x.Cvs.Count
            })
            .ToListAsync();

        return Ok(users);
    }

    // ==========================================
    // DEACTIVATE USER
    // ==========================================

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        user.IsActive = false;

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
    action: "Admin action",
    actorName: User.Identity?.Name ?? "Administrator",
    targetType: "User",
    targetId: user.Id.ToString(),
    details: $"User deactivated: {user.FullName}"
);

        return Ok(new
        {
            message = "User deactivated successfully.",
            id = user.Id,
            status = "inactive"
        });
    }

    // ==========================================
    // REACTIVATE USER
    // ==========================================

    [HttpPut("{id:int}/reactivate")]
    public async Task<IActionResult> ReactivateUser(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        user.IsActive = true;

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
    action: "Admin action",
    actorName: User.Identity?.Name ?? "Administrator",
    targetType: "User",
    targetId: user.Id.ToString(),
    details: $"User reactivated: {user.FullName}"
);

        return Ok(new
        {
            message = "User reactivated successfully.",
            id = user.Id,
            status = "active"
        });
    }
}