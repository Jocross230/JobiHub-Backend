namespace CVBuilder.API.Models;

public class Cv
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string ProfessionalTitle { get; set; } = string.Empty;

    public string ShortBio { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string LinkedInUrl { get; set; } = string.Empty;

    public string GitHubUrl { get; set; } = string.Empty;
    public bool IsSearchable { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // USER OWNERSHIP
    public int? UserId { get; set; }

    public User? User { get; set; }

    // CV CONTENT
    public ICollection<CvSkill> Skills { get; set; } = new List<CvSkill>();

    public ICollection<CvExperience> Experiences { get; set; } = new List<CvExperience>();

    public ICollection<CvProject> Projects { get; set; } = new List<CvProject>();

    public ICollection<CvEducation> Educations { get; set; }
        = new List<CvEducation>();
    public ICollection<Application> Applications { get; set; }
    = new List<Application>();
}