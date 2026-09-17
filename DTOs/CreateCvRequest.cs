namespace CVBuilder.API.DTOs;

public class CreateCvRequest
{
    public string FullName { get; set; } = string.Empty;

    public string ProfessionalTitle { get; set; } = string.Empty;

    public string ShortBio { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string LinkedInUrl { get; set; } = string.Empty;

    public string GitHubUrl { get; set; } = string.Empty;
}