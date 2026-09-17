using CVBuilder.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/admin/recruitment")]
[Authorize(Roles = "Admin")]
public class AdminRecruitmentController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public AdminRecruitmentController(
        CvBuilderDbContext context)
    {
        _context = context;
    }

    // Get all recruitment service requests
    [HttpGet]
    public async Task<IActionResult> GetAllRequests()
    {
        var requests = await _context.RecruitmentRequests
    .AsNoTracking()
    .Include(x => x.Business)
    .OrderByDescending(x => x.CreatedAt)
    .Select(x => new
    {
        id = x.Id,
        businessId = x.BusinessId,

        companyName = x.Business!.CompanyName,
        contactEmail = x.Business.ContactEmail,
        contactPhone = x.Business.ContactPhone,
        website = x.Business.Website,

        positionTitle = x.PositionTitle,
        numberOfCandidates = x.NumberOfCandidates,
        urgency = x.Urgency,
        employmentType = x.EmploymentType,
        experienceLevel = x.ExperienceLevel,
        location = x.Location,
        salaryRange = x.SalaryRange,

        jobDescription = x.JobDescription,
        requirements = x.Requirements,
        skills = x.Skills,
        additionalMessage = x.AdditionalMessage,

        status = x.Status,
        adminNotes = x.AdminNotes,

        createdAt = x.CreatedAt,
        updatedAt = x.UpdatedAt
    })
    .ToListAsync();

        return Ok(requests);
    }

    // Get a single recruitment request
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRequest(int id)
    {
        var request = await _context.RecruitmentRequests
            .AsNoTracking()
            .Include(x => x.Business)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                id = x.Id,

                businessId = x.BusinessId,

                // Company contact details
                companyName = x.Business!.CompanyName,
                contactEmail = x.Business.ContactEmail,
                contactPhone = x.Business.ContactPhone,
                website = x.Business.Website,
                companyLocation = x.Business.Location,

                // Recruitment request
                positionTitle = x.PositionTitle,
                numberOfCandidates = x.NumberOfCandidates,
                urgency = x.Urgency,
                employmentType = x.EmploymentType,
                experienceLevel = x.ExperienceLevel,
                location = x.Location,
                salaryRange = x.SalaryRange,

                jobDescription = x.JobDescription,
                requirements = x.Requirements,
                skills = x.Skills,
                additionalMessage = x.AdditionalMessage,

                status = x.Status,
                adminNotes = x.AdminNotes,

                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (request == null)
        {
            return NotFound(new
            {
                message = "Recruitment request not found."
            });
        }

        return Ok(request);
    }

    // Update recruitment request status
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateStatusRequest request)
    {
        var recruitmentRequest =
            await _context.RecruitmentRequests
                .FirstOrDefaultAsync(x => x.Id == id);

        if (recruitmentRequest == null)
        {
            return NotFound(new
            {
                message =
                    "Recruitment request not found."
            });
        }

        var allowedStatuses = new[]
 {
    "Pending",
    "Under Review",
    "In Progress",
    "Candidates Sourced",
    "Candidates Screened",
    "Shortlist Ready",
    "Interview Stage",
    "Filled",
    "Completed",
    "Declined"
};

        if (!allowedStatuses.Contains(request.Status))
        {
            return BadRequest(new
            {
                message = "Invalid recruitment request status."
            });
        }

        recruitmentRequest.Status =
            request.Status;

        recruitmentRequest.UpdatedAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Recruitment request status updated successfully.",

            id = recruitmentRequest.Id,

            status =
                recruitmentRequest.Status,

            updatedAt =
                recruitmentRequest.UpdatedAt
        });
    }

    // Add or update internal admin notes
    [HttpPut("{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes(
        int id,
        [FromBody] UpdateNotesRequest request)
    {
        var recruitmentRequest =
            await _context.RecruitmentRequests
                .FirstOrDefaultAsync(x => x.Id == id);

        if (recruitmentRequest == null)
        {
            return NotFound(new
            {
                message =
                    "Recruitment request not found."
            });
        }

        recruitmentRequest.AdminNotes =
            request.AdminNotes?.Trim()
            ?? string.Empty;

        recruitmentRequest.UpdatedAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Recruitment notes updated successfully.",

            id = recruitmentRequest.Id,

            adminNotes =
                recruitmentRequest.AdminNotes,

            updatedAt =
                recruitmentRequest.UpdatedAt
        });
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; }
            = string.Empty;
    }

    public class UpdateNotesRequest
    {
        public string AdminNotes { get; set; }
            = string.Empty;
    }
}