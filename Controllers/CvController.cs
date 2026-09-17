using System.Security.Claims;
using CVBuilder.API.Data;
using CVBuilder.API.DTOs;
using CVBuilder.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CVBuilder.API.Services;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CvController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public CvController(CvBuilderDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
    [HttpGet("my-cvs")]
    public async Task<IActionResult> GetMyCvs()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cvs = await _context.Cvs
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.ProfessionalTitle,
                x.Email,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(cvs);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCvRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cv = new Cv
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,

            FullName = request.FullName,
            ProfessionalTitle = request.ProfessionalTitle,
            ShortBio = request.ShortBio,
            Email = request.Email,
            Phone = request.Phone,
            Location = request.Location,
            LinkedInUrl = request.LinkedInUrl,
            GitHubUrl = request.GitHubUrl,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Cvs.Add(cv);

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
    action: "CV created",
    actorId: cv.UserId,
    actorName: cv.FullName,
    targetType: "CV",
    targetId: cv.Id.ToString(),
    details: $"New CV created: {cv.ProfessionalTitle}"
);

        return Ok(cv);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cv = await _context.Cvs
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId.Value);

        if (cv == null)
        {
            return NotFound("CV not found.");
        }

        return Ok(cv);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CreateCvRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cv = await _context.Cvs
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId.Value);

        if (cv == null)
        {
            return NotFound("CV not found.");
        }

        cv.FullName = request.FullName;
        cv.ProfessionalTitle = request.ProfessionalTitle;
        cv.ShortBio = request.ShortBio;
        cv.Email = request.Email;
        cv.Phone = request.Phone;
        cv.Location = request.Location;
        cv.LinkedInUrl = request.LinkedInUrl;
        cv.GitHubUrl = request.GitHubUrl;
        cv.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
    action: "CV updated",
    actorId: cv.UserId,
    actorName: cv.FullName,
    targetType: "CV",
    targetId: cv.Id.ToString(),
    details: $"CV updated: {cv.ProfessionalTitle}"
);

        return Ok(cv);
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cv = await _context.Cvs
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId.Value);

        if (cv == null)
        {
            return NotFound("CV not found.");
        }

        _context.Cvs.Remove(cv);

        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPost("{cvId:guid}/skills")]
    public async Task<IActionResult> AddSkill(
    Guid cvId,
    CvSkillRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cv = await _context.Cvs
            .FirstOrDefaultAsync(x =>
                x.Id == cvId &&
                x.UserId == userId.Value);

        if (cv == null)
        {
            return NotFound("CV not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Skill name is required.");
        }

        var skill = new CvSkill
        {
            Id = Guid.NewGuid(),
            CvId = cvId,
            Name = request.Name.Trim()
        };

        _context.CvSkills.Add(skill);

        await _context.SaveChangesAsync();

        return Ok(new CvSkillResponse
        {
            Id = skill.Id,
            CvId = skill.CvId,
            Name = skill.Name
        });
    }
    [HttpGet("{cvId:guid}/skills")]
    public async Task<IActionResult> GetSkills(Guid cvId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cvExists = await _context.Cvs
            .AnyAsync(x =>
                x.Id == cvId &&
                x.UserId == userId.Value);

        if (!cvExists)
        {
            return NotFound("CV not found.");
        }

        var skills = await _context.CvSkills
            .Where(x => x.CvId == cvId)
            .Select(x => new CvSkillResponse
            {
                Id = x.Id,
                CvId = x.CvId,
                Name = x.Name
            })
            .ToListAsync();

        return Ok(skills);
    }
    [HttpDelete("{cvId:guid}/skills/{skillId:guid}")]
    public async Task<IActionResult> DeleteSkill(Guid cvId, Guid skillId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var skill = await _context.CvSkills
            .Include(x => x.Cv)
            .FirstOrDefaultAsync(x =>
                x.Id == skillId &&
                x.CvId == cvId &&
                x.Cv.UserId == userId.Value);

        if (skill == null)
        {
            return NotFound("Skill not found.");
        }

        _context.CvSkills.Remove(skill);

        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPost("{cvId:guid}/experiences")]
    public async Task<IActionResult> AddExperience(
    Guid cvId,
    CvExperienceRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cvExists = await _context.Cvs
            .AnyAsync(x =>
                x.Id == cvId &&
                x.UserId == userId.Value);

        if (!cvExists)
        {
            return NotFound("CV not found.");
        }

        if (string.IsNullOrWhiteSpace(request.JobTitle))
        {
            return BadRequest("Job title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Company))
        {
            return BadRequest("Company is required.");
        }

        var experience = new CvExperience
        {
            Id = Guid.NewGuid(),
            CvId = cvId,
            JobTitle = request.JobTitle.Trim(),
            Company = request.Company.Trim(),
            Location = request.Location.Trim(),
            StartDate = request.StartDate,
            EndDate = request.IsCurrent ? null : request.EndDate,
            IsCurrent = request.IsCurrent,
            Description = request.Description.Trim()
        };

        _context.CvExperiences.Add(experience);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            experience.Id,
            experience.CvId,
            experience.JobTitle,
            experience.Company,
            experience.Location,
            experience.StartDate,
            experience.EndDate,
            experience.IsCurrent,
            experience.Description
        });
    }
    [HttpGet("{cvId:guid}/experiences")]
    public async Task<IActionResult> GetExperiences(Guid cvId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cvExists = await _context.Cvs
            .AnyAsync(x =>
                x.Id == cvId &&
                x.UserId == userId.Value);

        if (!cvExists)
        {
            return NotFound("CV not found.");
        }

        var experiences = await _context.CvExperiences
            .Where(x => x.CvId == cvId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new
            {
                x.Id,
                x.CvId,
                x.JobTitle,
                x.Company,
                x.Location,
                x.StartDate,
                x.EndDate,
                x.IsCurrent,
                x.Description
            })
            .ToListAsync();

        return Ok(experiences);
    }
    [HttpPut("{cvId:guid}/experiences/{experienceId:guid}")]
    public async Task<IActionResult> UpdateExperience(
    Guid cvId,
    Guid experienceId,
    CvExperienceRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var experience = await _context.CvExperiences
            .Include(x => x.Cv)
            .FirstOrDefaultAsync(x =>
                x.Id == experienceId &&
                x.CvId == cvId &&
                x.Cv.UserId == userId.Value);

        if (experience == null)
        {
            return NotFound("Experience not found.");
        }

        if (string.IsNullOrWhiteSpace(request.JobTitle))
        {
            return BadRequest("Job title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Company))
        {
            return BadRequest("Company is required.");
        }

        experience.JobTitle = request.JobTitle.Trim();
        experience.Company = request.Company.Trim();
        experience.Location = request.Location.Trim();
        experience.StartDate = request.StartDate;
        experience.EndDate = request.IsCurrent ? null : request.EndDate;
        experience.IsCurrent = request.IsCurrent;
        experience.Description = request.Description.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            experience.Id,
            experience.CvId,
            experience.JobTitle,
            experience.Company,
            experience.Location,
            experience.StartDate,
            experience.EndDate,
            experience.IsCurrent,
            experience.Description
        });
    }
    [HttpDelete("{cvId:guid}/experiences/{experienceId:guid}")]
    public async Task<IActionResult> DeleteExperience(
    Guid cvId,
    Guid experienceId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var experience = await _context.CvExperiences
            .Include(x => x.Cv)
            .FirstOrDefaultAsync(x =>
                x.Id == experienceId &&
                x.CvId == cvId &&
                x.Cv.UserId == userId.Value);

        if (experience == null)
        {
            return NotFound("Experience not found.");
        }

        _context.CvExperiences.Remove(experience);

        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPost("{cvId:guid}/projects")]
    public async Task<IActionResult> AddProject(
    Guid cvId,
    CvProjectRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cvExists = await _context.Cvs
            .AnyAsync(x =>
                x.Id == cvId &&
                x.UserId == userId.Value);

        if (!cvExists)
        {
            return NotFound("CV not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Project title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Project description is required.");
        }

        var project = new CvProject
        {
            Id = Guid.NewGuid(),
            CvId = cvId,
            Title = request.Title.Trim(),
            Role = request.Role.Trim(),
            Description = request.Description.Trim(),
            Technologies = request.Technologies.Trim(),
            ProjectUrl = request.ProjectUrl.Trim()
        };

        _context.CvProjects.Add(project);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            project.Id,
            project.CvId,
            project.Title,
            project.Role,
            project.Description,
            project.Technologies,
            project.ProjectUrl
        });
    }
    [HttpGet("{cvId:guid}/projects")]
    public async Task<IActionResult> GetProjects(Guid cvId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cvExists = await _context.Cvs
            .AnyAsync(x =>
                x.Id == cvId &&
                x.UserId == userId.Value);

        if (!cvExists)
        {
            return NotFound("CV not found.");
        }

        var projects = await _context.CvProjects
            .Where(x => x.CvId == cvId)
            .Select(x => new
            {
                x.Id,
                x.CvId,
                x.Title,
                x.Role,
                x.Description,
                x.Technologies,
                x.ProjectUrl
            })
            .ToListAsync();

        return Ok(projects);
    }
    [HttpPut("{cvId:guid}/projects/{projectId:guid}")]
    public async Task<IActionResult> UpdateProject(
    Guid cvId,
    Guid projectId,
    CvProjectRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var project = await _context.CvProjects
            .Include(x => x.Cv)
            .FirstOrDefaultAsync(x =>
                x.Id == projectId &&
                x.CvId == cvId &&
                x.Cv.UserId == userId.Value);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Project title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Project description is required.");
        }

        project.Title = request.Title.Trim();
        project.Role = request.Role.Trim();
        project.Description = request.Description.Trim();
        project.Technologies = request.Technologies.Trim();
        project.ProjectUrl = request.ProjectUrl.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            project.Id,
            project.CvId,
            project.Title,
            project.Role,
            project.Description,
            project.Technologies,
            project.ProjectUrl
        });
    }
    [HttpDelete("{cvId:guid}/projects/{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(
    Guid cvId,
    Guid projectId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var project = await _context.CvProjects
            .Include(x => x.Cv)
            .FirstOrDefaultAsync(x =>
                x.Id == projectId &&
                x.CvId == cvId &&
                x.Cv.UserId == userId.Value);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        _context.CvProjects.Remove(project);

        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPost("{cvId:guid}/educations")]
    public async Task<IActionResult> AddEducation(
    Guid cvId,
    CvEducationRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cvExists = await _context.Cvs
            .AnyAsync(x =>
                x.Id == cvId &&
                x.UserId == userId.Value);

        if (!cvExists)
        {
            return NotFound("CV not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Institution))
        {
            return BadRequest("Institution is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Degree))
        {
            return BadRequest("Degree is required.");
        }

        var education = new CvEducation
        {
            Id = Guid.NewGuid(),
            CvId = cvId,
            Institution = request.Institution.Trim(),
            Degree = request.Degree.Trim(),
            FieldOfStudy = request.FieldOfStudy.Trim(),
            Location = request.Location.Trim(),
            StartDate = request.StartDate,
            EndDate = request.IsCurrent ? null : request.EndDate,
            IsCurrent = request.IsCurrent
        };

        _context.CvEducations.Add(education);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            education.Id,
            education.CvId,
            education.Institution,
            education.Degree,
            education.FieldOfStudy,
            education.Location,
            education.StartDate,
            education.EndDate,
            education.IsCurrent
        });
    }
    [HttpGet("{cvId:guid}/educations")]
    public async Task<IActionResult> GetEducations(Guid cvId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var cvExists = await _context.Cvs
            .AnyAsync(x =>
                x.Id == cvId &&
                x.UserId == userId.Value);

        if (!cvExists)
        {
            return NotFound("CV not found.");
        }

        var educations = await _context.CvEducations
            .Where(x => x.CvId == cvId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new
            {
                x.Id,
                x.CvId,
                x.Institution,
                x.Degree,
                x.FieldOfStudy,
                x.Location,
                x.StartDate,
                x.EndDate,
                x.IsCurrent
            })
            .ToListAsync();

        return Ok(educations);
    }
    [HttpPut("{cvId:guid}/educations/{educationId:guid}")]
    public async Task<IActionResult> UpdateEducation(
    Guid cvId,
    Guid educationId,
    CvEducationRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var education = await _context.CvEducations
            .Include(x => x.Cv)
            .FirstOrDefaultAsync(x =>
                x.Id == educationId &&
                x.CvId == cvId &&
                x.Cv.UserId == userId.Value);

        if (education == null)
        {
            return NotFound("Education record not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Institution))
        {
            return BadRequest("Institution is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Degree))
        {
            return BadRequest("Degree is required.");
        }

        education.Institution = request.Institution.Trim();
        education.Degree = request.Degree.Trim();
        education.FieldOfStudy = request.FieldOfStudy.Trim();
        education.Location = request.Location.Trim();
        education.StartDate = request.StartDate;
        education.EndDate = request.IsCurrent ? null : request.EndDate;
        education.IsCurrent = request.IsCurrent;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            education.Id,
            education.CvId,
            education.Institution,
            education.Degree,
            education.FieldOfStudy,
            education.Location,
            education.StartDate,
            education.EndDate,
            education.IsCurrent
        });
    }
    [HttpDelete("{cvId:guid}/educations/{educationId:guid}")]
    public async Task<IActionResult> DeleteEducation(
    Guid cvId,
    Guid educationId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized("Unable to identify the logged-in user.");
        }

        var education = await _context.CvEducations
            .Include(x => x.Cv)
            .FirstOrDefaultAsync(x =>
                x.Id == educationId &&
                x.CvId == cvId &&
                x.Cv.UserId == userId.Value);

        if (education == null)
        {
            return NotFound("Education record not found.");
        }

        _context.CvEducations.Remove(education);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}