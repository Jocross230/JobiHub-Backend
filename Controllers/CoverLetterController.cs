using System.Security.Claims;
using CVBuilder.API.Data;
using CVBuilder.API.DTOs;
using CVBuilder.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoverLetterController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly GeminiService _geminiService;

    public CoverLetterController(
        CvBuilderDbContext context,
        GeminiService geminiService)
    {
        _context = context;
        _geminiService = geminiService;
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

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] CoverLetterRequest request)
    {
        // ---------------------------------------------------------
        // 1. Identify logged-in user
        // ---------------------------------------------------------

        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(
                "Unable to identify the logged-in user.");
        }

        // ---------------------------------------------------------
        // 2. Validate request
        // ---------------------------------------------------------

        if (request.CvId == Guid.Empty)
        {
            return BadRequest("CV ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.JobTitle))
        {
            return BadRequest("Job title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Company))
        {
            return BadRequest("Company name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.JobDescription))
        {
            return BadRequest("Job description is required.");
        }

        // ---------------------------------------------------------
        // 3. Get CV belonging to logged-in user
        // ---------------------------------------------------------

        var cv = await _context.Cvs
            .Include(x => x.Skills)
            .Include(x => x.Experiences)
            .Include(x => x.Projects)
            .Include(x => x.Educations)
            .FirstOrDefaultAsync(x =>
                x.Id == request.CvId &&
                x.UserId == userId.Value);

        if (cv == null)
        {
            return NotFound(
                "CV not found or you do not have access to this CV.");
        }

        // ---------------------------------------------------------
        // 4. Build candidate information
        // ---------------------------------------------------------

        var candidateInformation = $"""
        Name: {cv.FullName}
        Professional Title: {cv.ProfessionalTitle}
        Professional Summary: {cv.ShortBio}
        Email: {cv.Email}
        Location: {cv.Location}

        Skills:
        {string.Join(
            ", ",
            cv.Skills.Select(x => x.Name)
        )}

        Professional Experience:
        {string.Join(
            Environment.NewLine + Environment.NewLine,
            cv.Experiences.Select(x =>
                $"""
                {x.JobTitle} at {x.Company}
                Period: {x.StartDate:yyyy-MM} - {(x.IsCurrent
                    ? "Present"
                    : x.EndDate?.ToString("yyyy-MM") ?? "N/A")}

                Description:
                {x.Description}
                """
            )
        )}

        Projects:
        {string.Join(
            Environment.NewLine + Environment.NewLine,
            cv.Projects.Select(x =>
                $"""
                {x.Title}
                Role: {x.Role}

                Description:
                {x.Description}

                Technologies:
                {x.Technologies}
                """
            )
        )}

        Education:
        {string.Join(
            Environment.NewLine + Environment.NewLine,
            cv.Educations.Select(x =>
                $"""
                Degree: {x.Degree}
                Institution: {x.Institution}
                Field of Study: {x.FieldOfStudy}
                """
            )
        )}
        """;

        // ---------------------------------------------------------
        // 5. Generate cover letter
        // ---------------------------------------------------------

        try
        {
            var coverLetter =
                await _geminiService.GenerateCoverLetterAsync(
                    candidateInformation,
                    request.JobTitle.Trim(),
                    request.Company.Trim(),
                    request.JobDescription.Trim());

            // -----------------------------------------------------
            // 6. Return generated letter
            //
            // IMPORTANT:
            // Nothing is saved to the database.
            // -----------------------------------------------------

            return Ok(new CoverLetterResponse
            {
                CoverLetter = coverLetter
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("===== COVER LETTER ERROR =====");
            Console.WriteLine(ex);
            Console.WriteLine("==============================");

            return StatusCode(
                500,
                new
                {
                    message =
                        "Cover letter generation failed. Please try again."
                });
        }
    }
}