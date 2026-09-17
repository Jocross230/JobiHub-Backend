using CVBuilder.API.Data;
using CVBuilder.API.Models;
using CVBuilder.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public ApplicationController(CvBuilderDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
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

    // ============================================================
    // JOB SEEKER - APPLY FOR JOB
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> Apply(
    [FromBody] ApplyRequest request)
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
                message = "Job ID is required."
            });
        }

        if (request.CvId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Please select a CV before applying."
            });
        }

        // --------------------------------------------------------
        // Find published CareerFlow job
        // --------------------------------------------------------

        var job = await _context.Jobs
            .AsNoTracking()
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

        // --------------------------------------------------------
        // JOB READY PAYMENT CHECK
        // Only approved and non-expired Job Ready payments
        // can submit CareerFlow applications.
        // --------------------------------------------------------

        var now = DateTime.UtcNow;

        var hasJobReady = await _context.Payments
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == userId.Value &&
                x.Product == "JobReady" &&
                x.Status == "Approved" &&
                x.ExpiresAt != null &&
                x.ExpiresAt > now);

        if (!hasJobReady)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "An active CareerFlow Job Ready subscription is required to apply for this job.",
                product = "JobReady"
            });
        }

        // --------------------------------------------------------
        // Check closing date
        // --------------------------------------------------------

        if (job.ClosingDate.HasValue &&
            job.ClosingDate.Value < DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "This job application is closed."
            });
        }

        // --------------------------------------------------------
        // Verify CV belongs to logged-in user
        // --------------------------------------------------------

        var cv = await _context.Cvs
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.CvId &&
                x.UserId == userId.Value);

        if (cv == null)
        {
            return NotFound(new
            {
                message = "CV not found or you do not have access to this CV."
            });
        }

        // --------------------------------------------------------
        // Prevent duplicate application
        // --------------------------------------------------------

        var alreadyApplied = await _context.Applications
            .AnyAsync(x =>
                x.JobId == request.JobId &&
                x.UserId == userId.Value);

        if (alreadyApplied)
        {
            return BadRequest(new
            {
                message = "You have already applied for this job."
            });
        }

        // --------------------------------------------------------
        // Create application
        // --------------------------------------------------------

        var application = new Application
        {
            JobId = request.JobId,
            UserId = userId.Value,
            CvId = request.CvId,
            Status = "submitted",
            AppliedAt = DateTime.UtcNow
        };

        _context.Applications.Add(application);

        await _context.SaveChangesAsync();

        var applicant = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == application.UserId);

        await _activityLogService.LogAsync(
            action: "Application submitted",
            actorId: application.UserId,
            actorName: applicant?.FullName ?? "Job Seeker",
            targetType: "Application",
            targetId: application.Id.ToString(),
            details: $"Application submitted for: {job.Title}"
        );

        return Ok(new
        {
            message = "Application submitted successfully.",
            applicationId = application.Id,
            jobId = application.JobId,
            cvId = application.CvId,
            status = application.Status,
            appliedAt = application.AppliedAt
        });
    }

    // ============================================================
    // JOB SEEKER - GET MY APPLICATIONS
    // ============================================================

    [HttpGet("my")]
    public async Task<IActionResult> GetMyApplications()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var applications = await _context.Applications
            .AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.Cv)
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.AppliedAt)
            .Select(x => new
            {
                id = x.Id,
                jobId = x.JobId,
                cvId = x.CvId,
                status = x.Status,
                appliedAt = x.AppliedAt,

                job = x.Job == null
                    ? null
                    : new
                    {
                        id = x.Job.Id,
                        title = x.Job.Title,
                        location = x.Job.Location,
                        employmentType = x.Job.EmploymentType,
                        workArrangement = x.Job.WorkArrangement,
                        salary = x.Job.Salary
                    },

                cv = x.Cv == null
                    ? null
                    : new
                    {
                        id = x.Cv.Id,
                        fullName = x.Cv.FullName,
                        professionalTitle = x.Cv.ProfessionalTitle
                    }
            })
            .ToListAsync();

        return Ok(applications);
    }

    // ============================================================
    // EMPLOYER - GET APPLICANTS
    // ============================================================

    [HttpGet("/api/business/applicants")]
    [Authorize(Roles = "Business")]
    public async Task<IActionResult> GetBusinessApplicants()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        // --------------------------------------------------------
        // Find the business belonging to the logged-in user
        // --------------------------------------------------------

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId.Value);

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        // --------------------------------------------------------
        // Get applications only for this business's jobs
        // --------------------------------------------------------

        var applications = await _context.Applications
            .AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.Cv)
            .Where(x =>
                x.Job != null &&
                x.Job.BusinessId == business.Id)
            .OrderByDescending(x => x.AppliedAt)
            .Select(x => new
            {
                id = x.Id,

                jobId = x.JobId,

                cvId = x.CvId,

                status = x.Status,

                appliedAt = x.AppliedAt,

                job = x.Job == null
                    ? null
                    : new
                    {
                        id = x.Job.Id,
                        title = x.Job.Title,
                        location = x.Job.Location,
                        employmentType = x.Job.EmploymentType,
                        workArrangement = x.Job.WorkArrangement
                    },

                applicant = x.Cv == null
                    ? null
                    : new
                    {
                        cvId = x.Cv.Id,
                        fullName = x.Cv.FullName,
                        professionalTitle = x.Cv.ProfessionalTitle,
                        email = x.Cv.Email,
                        phone = x.Cv.Phone,
                        location = x.Cv.Location
                    },

                cv = x.Cv == null
                    ? null
                    : new
                    {
                        id = x.Cv.Id,
                        fullName = x.Cv.FullName,
                        professionalTitle = x.Cv.ProfessionalTitle
                    }
            })
            .ToListAsync();

        return Ok(applications);
    }
    [HttpGet("/api/business/applicants/{applicationId:int}/cv")]
    [Authorize(Roles = "Business")]
    public async Task<IActionResult> GetApplicantCv(int applicationId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId.Value);

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var application = await _context.Applications
            .AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.Cv)
                .ThenInclude(x => x.Skills)
            .Include(x => x.Cv)
                .ThenInclude(x => x.Experiences)
            .Include(x => x.Cv)
                .ThenInclude(x => x.Projects)
            .Include(x => x.Cv)
                .ThenInclude(x => x.Educations)
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId &&
                x.Job != null &&
                x.Job.BusinessId == business.Id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found or you do not have access to it."
            });
        }

        if (application.Cv == null)
        {
            return NotFound(new
            {
                message = "The CV submitted with this application could not be found."
            });
        }

        var cv = application.Cv;

        return Ok(new
        {
            application = new
            {
                id = application.Id,
                status = application.Status,
                appliedAt = application.AppliedAt,

                job = application.Job == null
                    ? null
                    : new
                    {
                        id = application.Job.Id,
                        title = application.Job.Title,
                        location = application.Job.Location,
                        employmentType = application.Job.EmploymentType,
                        workArrangement = application.Job.WorkArrangement
                    }
            },

            cv = new
            {
                id = cv.Id,
                userId = cv.UserId,
                fullName = cv.FullName,
                professionalTitle = cv.ProfessionalTitle,
                shortBio = cv.ShortBio,
                email = cv.Email,
                phone = cv.Phone,
                location = cv.Location,
                linkedInUrl = cv.LinkedInUrl,
                gitHubUrl = cv.GitHubUrl,
                createdAt = cv.CreatedAt,
                updatedAt = cv.UpdatedAt,

                skills = cv.Skills.Select(x => new
                {
                    id = x.Id,
                    name = x.Name
                }),

                experiences = cv.Experiences.Select(x => new
                {
                    id = x.Id,
                    jobTitle = x.JobTitle,
                    company = x.Company,
                    location = x.Location,
                    startDate = x.StartDate,
                    endDate = x.EndDate,
                    isCurrent = x.IsCurrent,
                    description = x.Description
                }),

                projects = cv.Projects.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    role = x.Role,
                    description = x.Description,
                    technologies = x.Technologies,
                    projectUrl = x.ProjectUrl
                }),

                educations = cv.Educations.Select(x => new
                {
                    id = x.Id,
                    institution = x.Institution,
                    degree = x.Degree,
                    fieldOfStudy = x.FieldOfStudy,
                    location = x.Location,
                    startDate = x.StartDate,
                    endDate = x.EndDate,
                    isCurrent = x.IsCurrent
                })
            }
        });
    }
    [HttpDelete("/api/business/applicants/{applicationId:int}")]
    [Authorize(Roles = "Business")]
    public async Task<IActionResult> DeleteBusinessApplicant(int applicationId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId.Value);

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var application = await _context.Applications
            .Include(x => x.Job)
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId &&
                x.Job != null &&
                x.Job.BusinessId == business.Id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found or you do not have access to it."
            });
        }

        _context.Applications.Remove(application);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Applicant removed successfully."
        });
    }
    [HttpPut("/api/business/applicants/{applicationId:int}/status")]
    [Authorize(Roles = "Business")]
    public async Task<IActionResult> UpdateApplicantStatus(
    int applicationId,
    [FromBody] UpdateApplicantStatusRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId.Value);

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var newStatus = request.Status?.Trim().ToLowerInvariant();

        var allowedStatuses = new[]
        {
        "submitted",
        "reviewing",
        "shortlisted",
        "rejected",
        "hired"
    };

        if (string.IsNullOrWhiteSpace(newStatus) ||
            !allowedStatuses.Contains(newStatus))
        {
            return BadRequest(new
            {
                message = "Invalid applicant status."
            });
        }

        var application = await _context.Applications
            .Include(x => x.Job)
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId &&
                x.Job != null &&
                x.Job.BusinessId == business.Id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found or you do not have access to it."
            });
        }

        application.Status = newStatus;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Applicant status updated successfully.",
            applicationId = application.Id,
            status = application.Status
        });
    }
}

public class ApplyRequest
{
    public int JobId { get; set; }

    public Guid CvId { get; set; }
}
public class UpdateApplicantStatusRequest
{
    public string Status { get; set; } = string.Empty;
}