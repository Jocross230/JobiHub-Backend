using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CVBuilder.API.Data;
using CVBuilder.API.Models;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/support")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public SupportController(CvBuilderDbContext context)
    {
        _context = context;
    }

    // POST: /api/support
    [HttpPost]
    public async Task<IActionResult> SubmitSupportRequest(
        [FromBody] SubmitSupportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return BadRequest(new
            {
                message = "Subject is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new
            {
                message = "Description is required."
            });
        }

        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify the logged-in user."
            });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "User account not found."
            });
        }

        var issue = new SupportIssue
        {
            ReporterName = user.FullName,
            Category = string.IsNullOrWhiteSpace(request.Category)
                ? "General"
                : request.Category,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Priority = string.IsNullOrWhiteSpace(request.Priority)
                ? "medium"
                : request.Priority,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        if (user.Role.Equals(
                "Business",
                StringComparison.OrdinalIgnoreCase))
        {
            var business = await _context.Businesses
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (business == null)
            {
                return BadRequest(new
                {
                    message = "Business profile not found."
                });
            }

            issue.BusinessId = business.Id;
            issue.UserId = userId;
        }
        else
        {
            issue.UserId = userId;
        }

        _context.SupportIssues.Add(issue);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Support request submitted successfully.",
            id = issue.Id,
            status = issue.Status,
            createdAt = issue.CreatedAt
        });
    }

    // GET: /api/support/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMySupportIssues()
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify the logged-in user."
            });
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "User account not found."
            });
        }

        IQueryable<SupportIssue> query =
            _context.SupportIssues.AsNoTracking();

        if (user.Role.Equals(
                "Business",
                StringComparison.OrdinalIgnoreCase))
        {
            var businessId = await _context.Businesses
                .Where(x => x.UserId == userId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (!businessId.HasValue)
            {
                return Ok(Array.Empty<object>());
            }

            query = query.Where(x =>
                x.BusinessId == businessId.Value);
        }
        else
        {
            query = query.Where(x =>
                x.UserId == userId);
        }

        var issues = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                userId = x.UserId.HasValue
                    ? x.UserId.Value.ToString()
                    : null,
                businessId = x.BusinessId.HasValue
                    ? x.BusinessId.Value.ToString()
                    : null,
                reporterName = x.ReporterName,
                category = x.Category,
                subject = x.Subject,
                description = x.Description,
                priority = x.Priority,
                status = x.Status,
                assignedTo = x.AssignedTo,
                createdAt = x.CreatedAt,
                resolution = x.Resolution
            })
            .ToListAsync();

        return Ok(issues);
    }
}

public class SubmitSupportRequest
{
    public string Category { get; set; } = "General";

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = "medium";
}