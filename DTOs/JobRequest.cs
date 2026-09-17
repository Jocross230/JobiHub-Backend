namespace CVBuilder.API.DTOs;

public class JobRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Responsibilities { get; set; } = string.Empty;

    public string Requirements { get; set; } = string.Empty;

    public List<string> Skills { get; set; } = new();

    public string Location { get; set; } = string.Empty;

    public string WorkArrangement { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;

    public string ExperienceLevel { get; set; } = string.Empty;

    public string Salary { get; set; } = string.Empty;

    public string ApplicationMethod { get; set; } = "careerflow";

    public DateTime? ClosingDate { get; set; }

    public string Status { get; set; } = "published";
}