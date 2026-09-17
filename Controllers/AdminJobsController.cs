using CVBuilder.API.Data;
using CVBuilder.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/Admin/jobs")]
[Authorize(Roles = "Admin")]
public class AdminJobsController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public AdminJobsController(CvBuilderDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    // GET: /api/Admin/jobs
    [HttpGet]
    public async Task<IActionResult> GetAllJobs()
    {
        var jobs = await _context.Jobs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                businessId = x.BusinessId,
                companyName = x.Business != null
                    ? x.Business.CompanyName
                    : "Unknown Company",
                title = x.Title,
                description = x.Description,
                location = x.Location,
                workArrangement = x.WorkArrangement,
                employmentType = x.EmploymentType,
                experienceLevel = x.ExperienceLevel,
                salary = x.Salary,
                applicationMethod = x.ApplicationMethod,
                closingDate = x.ClosingDate,
                status = x.Status,
                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(jobs);
    }

    // PUT: /api/Admin/jobs/{id}/status
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> ChangeJobStatus(
        int id,
        [FromBody] ChangeJobStatusRequest request)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        var allowedStatuses = new[]
        {
            "draft",
            "published",
            "closed"
        };

        if (string.IsNullOrWhiteSpace(request.Status) ||
            !allowedStatuses.Contains(
                request.Status.ToLower()))
        {
            return BadRequest(new
            {
                message = "Invalid job status. Use draft, published, or closed."
            });
        }
        var oldStatus = job.Status;

        job.Status = request.Status.ToLower();
        job.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
    action: "Admin action",
    actorName: User.Identity?.Name ?? "Administrator",
    targetType: "Job",
    targetId: job.Id.ToString(),
    details: $"Job status changed: {job.Title} ({oldStatus} → {job.Status})"
);

        return Ok(new
        {
            message = "Job status updated successfully.",
            id = job.Id,
            status = job.Status,
            updatedAt = job.UpdatedAt
        });
    }

    // DELETE: /api/Admin/jobs/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemoveJob(int id)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        var jobTitle = job.Title;

        _context.Jobs.Remove(job);

        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            action: "Admin action",
            actorName: User.Identity?.Name ?? "Administrator",
            targetType: "Job",
            targetId: job.Id.ToString(),
            details: $"Job removed: {jobTitle}"
        );

        return Ok(new
        {
            message = "Job removed successfully.",
            id = id
        });
    }
}

public class ChangeJobStatusRequest
{
    public string Status { get; set; } = string.Empty;
}