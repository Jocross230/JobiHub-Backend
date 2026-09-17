using CVBuilder.API.Data;
using CVBuilder.API.DTOs;
using CVBuilder.API.Models;
using CVBuilder.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/business/jobs")]
[Authorize(Roles = "Business")]
public class BusinessJobsController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public BusinessJobsController(CvBuilderDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] JobRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var business = await _context.Businesses
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Please complete your company profile before posting a job."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Job title is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new
            {
                message = "Job description is required."
            });
        }

        var job = new Job
        {
            BusinessId = business.Id,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Responsibilities = request.Responsibilities?.Trim() ?? string.Empty,
            Requirements = request.Requirements?.Trim() ?? string.Empty,
            Skills = JsonSerializer.Serialize(request.Skills ?? new List<string>()),
            Location = request.Location?.Trim() ?? string.Empty,
            WorkArrangement = request.WorkArrangement?.Trim() ?? string.Empty,
            EmploymentType = request.EmploymentType?.Trim() ?? string.Empty,
            ExperienceLevel = request.ExperienceLevel?.Trim() ?? string.Empty,
            Salary = request.Salary?.Trim() ?? string.Empty,
            ApplicationMethod = request.ApplicationMethod?.Trim() ?? "careerflow",
            ClosingDate = request.ClosingDate,
            Status = string.IsNullOrWhiteSpace(request.Status)
                ? "published"
                : request.Status.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Jobs.Add(job);

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
    action: "Job posted",
    actorId: job.Business?.UserId,
    actorName: job.Business?.CompanyName ?? "Business",
    targetType: "Job",
    targetId: job.Id.ToString(),
    details: $"Job posted: {job.Title}"
);

        return Ok(new
        {
            message = "Job posted successfully.",
            job
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyJobs()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (business == null)
        {
            return Ok(new List<Job>());
        }

        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(x => x.BusinessId == business.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(jobs);
    }
    [AllowAnonymous]
    [HttpGet("/api/jobs")]
    public async Task<IActionResult> GetPublishedJobs()
    {
        var jobs = await _context.Jobs
            .AsNoTracking()
            .Include(x => x.Business)
            .Where(x => x.Status == "published")
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(jobs.Select(job => new
        {
            id = job.Id,
            title = job.Title,
            company = job.Business?.CompanyName ?? "Company",
            location = job.Location,
            workArrangement = job.WorkArrangement,
            employmentType = job.EmploymentType,
            experienceLevel = job.ExperienceLevel,
            salary = job.Salary,
            description = job.Description,
            responsibilities = job.Responsibilities,
            requirements = job.Requirements,
            skills = string.IsNullOrWhiteSpace(job.Skills)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(job.Skills) ?? new List<string>(),
            source = "careerflow",
            postedAt = job.CreatedAt,
            closingDate = job.ClosingDate,
            status = job.Status,
            businessId = job.BusinessId
        }));
    }

    [AllowAnonymous]
    [HttpGet("/api/jobs/{id:int}")]
    public async Task<IActionResult> GetJobById(int id)
    {
        var job = await _context.Jobs
            .AsNoTracking()
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == "published");

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        return Ok(new
        {
            id = job.Id,
            title = job.Title,
            company = job.Business?.CompanyName ?? "Company",
            location = job.Location,
            workArrangement = job.WorkArrangement,
            employmentType = job.EmploymentType,
            experienceLevel = job.ExperienceLevel,
            salary = job.Salary,
            description = job.Description,
            responsibilities = job.Responsibilities,
            requirements = job.Requirements,
            skills = string.IsNullOrWhiteSpace(job.Skills)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(job.Skills) ?? new List<string>(),
            source = "careerflow",
            postedAt = job.CreatedAt,
            closingDate = job.ClosingDate,
            status = job.Status,
            businessId = job.BusinessId
        });
    }
}