namespace CVBuilder.API.Models;

public class Job
{
    public int Id { get; set; }

    public int BusinessId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Responsibilities { get; set; } = string.Empty;

    public string Requirements { get; set; } = string.Empty;

    public string Skills { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string WorkArrangement { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;

    public string ExperienceLevel { get; set; } = string.Empty;

    public string Salary { get; set; } = string.Empty;

    public string ApplicationMethod { get; set; } = "careerflow";

    public DateTime? ClosingDate { get; set; }

    public string Status { get; set; } = "draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Business? Business { get; set; }
}