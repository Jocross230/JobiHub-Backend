using System.Security.Claims;
using CVBuilder.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/business/candidates")]
[Authorize(Roles = "Business")]
public class CandidateController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public CandidateController(CvBuilderDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> SearchCandidates(
        [FromQuery] string? query,
        [FromQuery] string? location,
        [FromQuery] string? title)
    {
        var candidates = await _context.Cvs
            .AsNoTracking()
            .Include(x => x.Skills)
            .Where(x =>
                x.UserId != null &&
                x.IsSearchable)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var search = query.Trim().ToLower();

            candidates = candidates
                .Where(x =>
                    x.FullName.ToLower().Contains(search) ||
                    x.ProfessionalTitle.ToLower().Contains(search) ||
                    x.ShortBio.ToLower().Contains(search) ||
                    x.Location.ToLower().Contains(search) ||
                    x.Skills.Any(s =>
                        s.Name.ToLower().Contains(search)))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var searchLocation = location.Trim().ToLower();

            candidates = candidates
                .Where(x =>
                    x.Location.ToLower().Contains(searchLocation))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            var searchTitle = title.Trim().ToLower();

            candidates = candidates
                .Where(x =>
                    x.ProfessionalTitle
                        .ToLower()
                        .Contains(searchTitle))
                .ToList();
        }

        var result = candidates
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                id = x.Id,
                fullName = x.FullName,
                professionalTitle = x.ProfessionalTitle,
                shortBio = x.ShortBio,
                location = x.Location,

                skills = x.Skills
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name
                    })
                    .ToList()
            })
            .ToList();

        return Ok(result);
    }

    // Get the current user's CV visibility settings
    [HttpGet("/api/candidates/my-visibility")]
    [Authorize]
    public async Task<IActionResult> GetMyVisibility()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var cvs = await _context.Cvs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                id = x.Id,
                fullName = x.FullName,
                isSearchable = x.IsSearchable
            })
            .OrderBy(x => x.fullName)
            .ToListAsync();

        return Ok(cvs);
    }

    // Update whether a CV can be discovered by employers
    [HttpPut("/api/candidates/{cvId:guid}/visibility")]
    [Authorize]
    public async Task<IActionResult> UpdateVisibility(
        Guid cvId,
        [FromBody] VisibilityRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var cv = await _context.Cvs
            .FirstOrDefaultAsync(x =>
                x.Id == cvId &&
                x.UserId == userId);

        if (cv == null)
        {
            return NotFound(new
            {
                message = "CV not found."
            });
        }

        cv.IsSearchable = request.IsSearchable;
        cv.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsSearchable
                ? "Your CV is now searchable by employers."
                : "Your CV is no longer searchable by employers.",

            cvId = cv.Id,
            isSearchable = cv.IsSearchable
        });
    }
    [HttpGet("{cvId:guid}")]
    public async Task<IActionResult> GetCandidateCv(Guid cvId)
    {
        var cv = await _context.Cvs
            .AsNoTracking()
            .Include(x => x.Skills)
            .Include(x => x.Experiences)
            .Include(x => x.Projects)
            .Include(x => x.Educations)
            .FirstOrDefaultAsync(x =>
                x.Id == cvId &&
                x.UserId != null &&
                x.IsSearchable);

        if (cv == null)
        {
            return NotFound(new
            {
                message = "Candidate CV not found or is not available for employer search."
            });
        }

        return Ok(new
        {
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

                skills = cv.Skills
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name
                    })
                    .ToList(),

                experiences = cv.Experiences
                    .Select(x => new
                    {
                        id = x.Id,
                        jobTitle = x.JobTitle,
                        company = x.Company,
                        location = x.Location,
                        startDate = x.StartDate,
                        endDate = x.EndDate,
                        isCurrent = x.IsCurrent,
                        description = x.Description
                    })
                    .ToList(),

                projects = cv.Projects
                    .Select(x => new
                    {
                        id = x.Id,
                        title = x.Title,
                        role = x.Role,
                        description = x.Description,
                        technologies = x.Technologies,
                        projectUrl = x.ProjectUrl
                    })
                    .ToList(),

                educations = cv.Educations
                    .Select(x => new
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
                    .ToList()
            }
        });
    }

    public class VisibilityRequest
    {
        public bool IsSearchable { get; set; }
    }
}