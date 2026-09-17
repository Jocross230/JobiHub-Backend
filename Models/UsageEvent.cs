namespace CVBuilder.API.Models;

public class UsageEvent
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string VisitorId { get; set; } = string.Empty;

    public Guid? CvId { get; set; }

    public DateTime CreatedAt { get; set; }
}