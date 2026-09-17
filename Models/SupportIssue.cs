namespace CVBuilder.API.Models;

public class SupportIssue
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? BusinessId { get; set; }

    public string ReporterName { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = "medium";

    public string Status { get; set; } = "Open";

    public string AssignedTo { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Resolution { get; set; } = string.Empty;

    public User? User { get; set; }

    public Business? Business { get; set; }
}