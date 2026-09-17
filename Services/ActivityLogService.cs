using CVBuilder.API.Data;
using CVBuilder.API.Models;

namespace CVBuilder.API.Services;

public class ActivityLogService
{
    private readonly CvBuilderDbContext _context;

    public ActivityLogService(CvBuilderDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string action,
        int? actorId = null,
        string actorName = "",
        string targetType = "",
        string targetId = "",
        string details = "")
    {
        var log = new ActivityLog
        {
            Action = action,
            ActorId = actorId,
            ActorName = actorName,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            OccurredAt = DateTime.UtcNow
        };

        _context.ActivityLogs.Add(log);

        await _context.SaveChangesAsync();
    }
}