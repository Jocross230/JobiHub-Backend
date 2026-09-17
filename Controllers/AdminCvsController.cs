using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CVBuilder.API.Data;
using CVBuilder.API.Services;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/Admin/cvs")]
[Authorize(Roles = "Admin")]
public class AdminCvsController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public AdminCvsController(CvBuilderDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    // GET: /api/Admin/cvs
    [HttpGet]
    public async Task<IActionResult> GetCvs()
    {
        var cvs = await _context.Cvs
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                userId = x.UserId,
                fullName = x.FullName,
                professionalTitle = x.ProfessionalTitle,
                shortBio = x.ShortBio,
                email = x.Email,
                phone = x.Phone,
                location = x.Location,
                linkedInUrl = x.LinkedInUrl,
                gitHubUrl = x.GitHubUrl,
                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(cvs);
    }

    // DELETE: /api/Admin/cvs/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCv(Guid id)
    {
        var cv = await _context.Cvs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cv == null)
        {
            return NotFound(new
            {
                message = "CV not found."
            });
        }

        _context.Cvs.Remove(cv);

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
    action: "Admin action",
    actorName: User.Identity?.Name ?? "Administrator",
    targetType: "CV",
    targetId: cv.Id.ToString(),
    details: $"CV deleted: {cv.FullName} - {cv.ProfessionalTitle}"
);

        return Ok(new
        {
            message = "CV deleted successfully.",
            id = id
        });
    }
}