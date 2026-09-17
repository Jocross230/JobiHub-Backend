using CVBuilder.API.Data;
using CVBuilder.API.Models;
using CVBuilder.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/business/recruitment")]
[Authorize(Roles = "Business")]
public class RecruitmentRequestsController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public RecruitmentRequestsController(CvBuilderDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    private bool TryGetUserId(out int userId)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        return int.TryParse(userIdClaim, out userId);
    }

    private async Task<Business?> GetCurrentBusiness()
    {
        if (!TryGetUserId(out var userId))
        {
            return null;
        }

        return await _context.Businesses
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    // Submit a recruitment service request
    [HttpPost]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateRecruitmentRequest request)
    {
        var business = await GetCurrentBusiness();

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        if (string.IsNullOrWhiteSpace(request.PositionTitle))
        {
            return BadRequest(new
            {
                message = "Position / Job Title is required."
            });
        }

        if (request.NumberOfCandidates < 1)
        {
            return BadRequest(new
            {
                message = "Number of candidates must be at least 1."
            });
        }

        var recruitmentRequest = new RecruitmentRequest
        {
            BusinessId = business.Id,

            PositionTitle = request.PositionTitle.Trim(),

            NumberOfCandidates = request.NumberOfCandidates,

            Urgency = request.Urgency?.Trim() ?? "Medium",

            EmploymentType =
                request.EmploymentType?.Trim()
                ?? "Full-time",

            ExperienceLevel =
                request.ExperienceLevel?.Trim()
                ?? "Mid-level",

            Location =
                request.Location?.Trim()
                ?? string.Empty,

            SalaryRange =
                request.SalaryRange?.Trim()
                ?? string.Empty,

            JobDescription =
                request.JobDescription?.Trim()
                ?? string.Empty,

            Requirements =
                request.Requirements?.Trim()
                ?? string.Empty,

            Skills =
                request.Skills?.Trim()
                ?? string.Empty,

            AdditionalMessage =
                request.AdditionalMessage?.Trim()
                ?? string.Empty,

            Status = "Pending",

            AdminNotes = string.Empty,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };

        _context.RecruitmentRequests.Add(
            recruitmentRequest
        );

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
    action: "Recruitment request submitted",
    actorId: business.UserId,
    actorName: business.CompanyName,
    targetType: "RecruitmentRequest",
    targetId: recruitmentRequest.Id.ToString(),
    details: $"Recruitment request submitted for: {recruitmentRequest.PositionTitle}"
);

        return Ok(new
        {
            message =
                "Your recruitment request has been submitted successfully.",

            id = recruitmentRequest.Id,

            status = recruitmentRequest.Status,

            createdAt = recruitmentRequest.CreatedAt
        });
    }

    // Get recruitment requests submitted by the current business
    [HttpGet]
    public async Task<IActionResult> GetMyRequests()
    {
        var business = await GetCurrentBusiness();

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var requests = await _context.RecruitmentRequests
            .AsNoTracking()
            .Where(x => x.BusinessId == business.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id,

                positionTitle = x.PositionTitle,

                numberOfCandidates =
                    x.NumberOfCandidates,

                urgency = x.Urgency,

                employmentType =
                    x.EmploymentType,

                experienceLevel =
                    x.ExperienceLevel,

                location = x.Location,

                salaryRange =
                    x.SalaryRange,

                jobDescription =
                    x.JobDescription,

                requirements =
                    x.Requirements,

                skills = x.Skills,

                additionalMessage =
                    x.AdditionalMessage,

                status = x.Status,

                adminNotes = x.AdminNotes,

                createdAt = x.CreatedAt,

                updatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(requests);
    }

    // Get one recruitment request
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRequest(int id)
    {
        var business = await GetCurrentBusiness();

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var request = await _context.RecruitmentRequests
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.BusinessId == business.Id)
            .Select(x => new
            {
                id = x.Id,

                positionTitle =
                    x.PositionTitle,

                numberOfCandidates =
                    x.NumberOfCandidates,

                urgency =
                    x.Urgency,

                employmentType =
                    x.EmploymentType,

                experienceLevel =
                    x.ExperienceLevel,

                location =
                    x.Location,

                salaryRange =
                    x.SalaryRange,

                jobDescription =
                    x.JobDescription,

                requirements =
                    x.Requirements,

                skills =
                    x.Skills,

                additionalMessage =
                    x.AdditionalMessage,

                status =
                    x.Status,

                adminNotes =
                    x.AdminNotes,

                createdAt =
                    x.CreatedAt,

                updatedAt =
                    x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (request == null)
        {
            return NotFound(new
            {
                message =
                    "Recruitment request not found."
            });
        }

        return Ok(request);
    }

    public class CreateRecruitmentRequest
    {
        public string PositionTitle { get; set; }
            = string.Empty;

        public int NumberOfCandidates { get; set; }
            = 1;

        public string Urgency { get; set; }
            = "Medium";

        public string EmploymentType { get; set; }
            = "Full-time";

        public string ExperienceLevel { get; set; }
            = "Mid-level";

        public string Location { get; set; }
            = string.Empty;

        public string SalaryRange { get; set; }
            = string.Empty;

        public string JobDescription { get; set; }
            = string.Empty;

        public string Requirements { get; set; }
            = string.Empty;

        public string Skills { get; set; }
            = string.Empty;

        public string AdditionalMessage { get; set; }
            = string.Empty;
    }
}