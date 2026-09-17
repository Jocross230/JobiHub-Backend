using CVBuilder.API.Data;
using CVBuilder.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public AnalyticsController(CvBuilderDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpPost("event")]
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventRequest request)
    {
        var allowedEvents = new[]
        {
            "CV_STARTED",
            "CV_DOWNLOADED",
            "COVER_LETTER_GENERATED"
        };

        if (!allowedEvents.Contains(request.EventType))
        {
            return BadRequest("Invalid event type.");
        }

        var usageEvent = new UsageEvent
        {
            Id = Guid.NewGuid(),
            EventType = request.EventType,
            VisitorId = request.VisitorId ?? string.Empty,
            CvId = request.CvId,
            CreatedAt = DateTime.UtcNow
        };

        _context.UsageEvents.Add(usageEvent);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Event recorded."
        });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateTime.UtcNow.Date;

        var tomorrow = today.AddDays(1);

        var todayEvents = await _context.UsageEvents
            .Where(x => x.CreatedAt >= today &&
                        x.CreatedAt < tomorrow)
            .ToListAsync();

        return Ok(new
        {
            date = today,
            cvStarted = todayEvents.Count(x => x.EventType == "CV_STARTED"),
            cvDownloaded = todayEvents.Count(x => x.EventType == "CV_DOWNLOADED"),
            coverLettersGenerated = todayEvents.Count(x => x.EventType == "COVER_LETTER_GENERATED")
        });
    }
    [HttpGet("all-time")]
    public async Task<IActionResult> GetAllTimeSummary()
    {
        var allEvents = await _context.UsageEvents
            .ToListAsync();

        return Ok(new
        {
            totalCvStarted = allEvents.Count(x => x.EventType == "CV_STARTED"),
            totalCvDownloaded = allEvents.Count(x => x.EventType == "CV_DOWNLOADED"),
            totalCoverLettersGenerated =
                allEvents.Count(x => x.EventType == "COVER_LETTER_GENERATED")
        });
    }
}


public class TrackEventRequest
{
    public string EventType { get; set; } = string.Empty;

    public string? VisitorId { get; set; }

    public Guid? CvId { get; set; }
}