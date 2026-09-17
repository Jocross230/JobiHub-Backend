using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CVBuilder.API.Data;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/Admin/activity")]
[Authorize(Roles = "Admin")]
public class AdminActivityController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public AdminActivityController(CvBuilderDbContext context)
    {
        _context = context;
    }

    // GET: /api/Admin/activity
    [HttpGet]
    public async Task<IActionResult> GetActivityLogs()
    {
        var logs = await _context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                action = x.Action,
                actorId = x.ActorId.HasValue
                    ? x.ActorId.Value.ToString()
                    : null,
                actorName = x.ActorName,
                targetType = x.TargetType,
                targetId = x.TargetId,
                details = x.Details,
                occurredAt = x.OccurredAt
            })
            .ToListAsync();

        return Ok(logs);
    }
}