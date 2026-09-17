using System.Security.Claims;
using CVBuilder.API.Data;
using CVBuilder.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/business/saved-candidates")]
[Authorize(Roles = "Business")]
public class SavedCandidatesController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public SavedCandidatesController(CvBuilderDbContext context)
    {
        _context = context;
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

    // Save a candidate
    [HttpPost]
    public async Task<IActionResult> SaveCandidate(
        [FromBody] SaveCandidateRequest request)
    {
        var business = await GetCurrentBusiness();

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var cv = await _context.Cvs
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.CvId &&
                x.UserId != null &&
                x.IsSearchable);

        if (cv == null)
        {
            return NotFound(new
            {
                message = "Candidate not found or is not available."
            });
        }

        var alreadySaved = await _context.SavedCandidates
            .AnyAsync(x =>
                x.BusinessId == business.Id &&
                x.CvId == request.CvId);

        if (alreadySaved)
        {
            return BadRequest(new
            {
                message = "Candidate is already saved."
            });
        }

        var savedCandidate = new SavedCandidate
        {
            BusinessId = business.Id,
            CvId = request.CvId,
            SavedAt = DateTime.UtcNow
        };

        _context.SavedCandidates.Add(savedCandidate);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Candidate saved successfully.",
            id = savedCandidate.Id,
            cvId = savedCandidate.CvId,
            savedAt = savedCandidate.SavedAt
        });
    }

    // Get saved candidates
    [HttpGet]
    public async Task<IActionResult> GetSavedCandidates()
    {
        var business = await GetCurrentBusiness();

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var savedCandidates = await _context.SavedCandidates
            .AsNoTracking()
            .Include(x => x.Cv)
                .ThenInclude(x => x!.Skills)
            .Where(x => x.BusinessId == business.Id)
            .OrderByDescending(x => x.SavedAt)
            .Select(x => new
            {
                id = x.Id,
                cvId = x.CvId,
                savedAt = x.SavedAt,

                candidate = new
                {
                    id = x.Cv!.Id,
                    fullName = x.Cv.FullName,
                    professionalTitle = x.Cv.ProfessionalTitle,
                    shortBio = x.Cv.ShortBio,
                    location = x.Cv.Location,

                    skills = x.Cv.Skills
                        .Select(s => new
                        {
                            id = s.Id,
                            name = s.Name
                        })
                        .ToList()
                }
            })
            .ToListAsync();

        return Ok(savedCandidates);
    }

    // Check whether a candidate is saved
    [HttpGet("check/{cvId:guid}")]
    public async Task<IActionResult> CheckSavedCandidate(Guid cvId)
    {
        var business = await GetCurrentBusiness();

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var saved = await _context.SavedCandidates
            .AnyAsync(x =>
                x.BusinessId == business.Id &&
                x.CvId == cvId);

        return Ok(new
        {
            cvId,
            saved
        });
    }

    // Remove a saved candidate
    [HttpDelete("{cvId:guid}")]
    public async Task<IActionResult> RemoveSavedCandidate(
        Guid cvId)
    {
        var business = await GetCurrentBusiness();

        if (business == null)
        {
            return BadRequest(new
            {
                message = "Business profile not found."
            });
        }

        var savedCandidate = await _context.SavedCandidates
            .FirstOrDefaultAsync(x =>
                x.BusinessId == business.Id &&
                x.CvId == cvId);

        if (savedCandidate == null)
        {
            return NotFound(new
            {
                message = "Saved candidate not found."
            });
        }

        _context.SavedCandidates.Remove(savedCandidate);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Candidate removed from saved candidates."
        });
    }

    public class SaveCandidateRequest
    {
        public Guid CvId { get; set; }
    }
}