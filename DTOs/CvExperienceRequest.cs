namespace CVBuilder.API.DTOs;

public class CvExperienceRequest
{
    public string JobTitle { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string Description { get; set; } = string.Empty;
}