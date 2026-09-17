namespace CVBuilder.API.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public string Action { get; set; } = string.Empty;

    public int? ActorId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public User? Actor { get; set; }
}