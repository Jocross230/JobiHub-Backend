using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CVBuilder.API.Data;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/Admin/support")]
[Authorize(Roles = "Admin")]
public class AdminSupportController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public AdminSupportController(CvBuilderDbContext context)
    {
        _context = context;
    }

    // GET: /api/Admin/support
    [HttpGet]
    public async Task<IActionResult> GetSupportIssues()
    {
        var issues = await _context.SupportIssues
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                userId = x.UserId.HasValue
                    ? x.UserId.Value.ToString()
                    : null,
                businessId = x.BusinessId.HasValue
                    ? x.BusinessId.Value.ToString()
                    : null,
                reporterName = x.ReporterName,
                category = x.Category,
                subject = x.Subject,
                description = x.Description,
                priority = x.Priority,
                status = x.Status,
                assignedTo = x.AssignedTo,
                createdAt = x.CreatedAt,
                resolution = x.Resolution
            })
            .ToListAsync();

        return Ok(issues);
    }

    // PUT: /api/Admin/support/{id}/status
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateIssueStatus(
        int id,
        [FromBody] UpdateSupportStatusRequest request)
    {
        var issue = await _context.SupportIssues
            .FirstOrDefaultAsync(x => x.Id == id);

        if (issue == null)
        {
            return NotFound(new
            {
                message = "Support issue not found."
            });
        }

        var allowedStatuses = new[]
        {
            "Open",
            "In Progress",
            "Waiting for User",
            "Resolved",
            "Closed"
        };

        if (string.IsNullOrWhiteSpace(request.Status) ||
            !allowedStatuses.Contains(request.Status))
        {
            return BadRequest(new
            {
                message = "Invalid support issue status."
            });
        }

        issue.Status = request.Status;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Support issue status updated successfully.",
            id = issue.Id,
            status = issue.Status
        });
    }
}

public class UpdateSupportStatusRequest
{
    public string Status { get; set; } = string.Empty;
}