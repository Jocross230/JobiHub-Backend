using System.Security.Claims;
using CVBuilder.API.Data;
using CVBuilder.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/Jobs/saved")]
[Authorize]
public class SavedJobsController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public SavedJobsController(CvBuilderDbContext context)
    {
        _context = context;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    // GET /api/Jobs/saved
    [HttpGet]
    public async Task<IActionResult> GetSavedJobs()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "Unable to identify the logged-in user."
            });
        }

        var savedJobs = await _context.SavedJobs
            .Where(x => x.UserId == userId.Value)
            .Include(x => x.Job)
                .ThenInclude(x => x!.Business)
            .OrderByDescending(x => x.SavedAt)
            .Select(x => new
            {
                id = x.Id,
                jobId = x.JobId,
                savedAt = x.SavedAt,

                job = new
                {
                    id = x.Job!.Id,
                    title = x.Job.Title,

                    company = x.Job.Business != null
                        ? x.Job.Business.CompanyName
                        : "CareerFlow Employer",

                    location = x.Job.Location,

                    workArrangement =
                        x.Job.WorkArrangement,

                    employmentType =
                        x.Job.EmploymentType,

                    experienceLevel =
                        x.Job.ExperienceLevel,

                    salary = x.Job.Salary,

                    description =
                        x.Job.Description,

                    responsibilities =
                        x.Job.Responsibilities,

                    requirements =
                        x.Job.Requirements,

                    skills = string.IsNullOrWhiteSpace(x.Job.Skills)
                        ? new List<string>()
                        : x.Job.Skills
                            .Split(
                                ',',
                                StringSplitOptions.RemoveEmptyEntries |
                                StringSplitOptions.TrimEntries)
                            .ToList(),

                    source = "careerflow",

                    externalUrl = (string?)null,

                    postedAt = x.Job.CreatedAt,

                    closingDate = x.Job.ClosingDate,

                    status = x.Job.Status,

                    businessId =
                        x.Job.BusinessId.ToString()
                }
            })
            .ToListAsync();

        return Ok(savedJobs);
    }

    // POST /api/Jobs/saved
    [HttpPost]
    public async Task<IActionResult> SaveJob(
        [FromBody] SaveJobRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "Unable to identify the logged-in user."
            });
        }

        if (request.JobId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid JobId is required."
            });
        }

        var job = await _context.Jobs
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x =>
                x.Id == request.JobId &&
                x.Status == "published");

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        var alreadySaved = await _context.SavedJobs
            .FirstOrDefaultAsync(x =>
                x.UserId == userId.Value &&
                x.JobId == request.JobId);

        if (alreadySaved != null)
        {
            return Ok(new
            {
                id = alreadySaved.Id,
                jobId = alreadySaved.JobId,
                savedAt = alreadySaved.SavedAt,
                message = "Job is already saved."
            });
        }

        var savedJob = new SavedJob
        {
            UserId = userId.Value,
            JobId = request.JobId,
            SavedAt = DateTime.UtcNow
        };

        _context.SavedJobs.Add(savedJob);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = savedJob.Id,
            jobId = savedJob.JobId,
            savedAt = savedJob.SavedAt,
            message = "Job saved successfully."
        });
    }

    // DELETE /api/Jobs/saved/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemoveSavedJob(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "Unable to identify the logged-in user."
            });
        }

        var savedJob = await _context.SavedJobs
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId.Value);

        if (savedJob == null)
        {
            return NotFound(new
            {
                message = "Saved job not found."
            });
        }

        _context.SavedJobs.Remove(savedJob);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Job removed from saved jobs."
        });
    }
}

public class SaveJobRequest
{
    public int JobId { get; set; }
}