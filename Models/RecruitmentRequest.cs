namespace CVBuilder.API.Models;

public class RecruitmentRequest
{
    public int Id { get; set; }

    public int BusinessId { get; set; }

    public string PositionTitle { get; set; } = string.Empty;

    public int NumberOfCandidates { get; set; } = 1;

    public string Urgency { get; set; } = "Medium";

    public string EmploymentType { get; set; } = "Full-time";

    public string ExperienceLevel { get; set; } = "Mid-level";

    public string Location { get; set; } = string.Empty;

    public string SalaryRange { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;

    public string Requirements { get; set; } = string.Empty;

    public string Skills { get; set; } = string.Empty;

    public string AdditionalMessage { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string AdminNotes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Business? Business { get; set; }
}