namespace CVBuilder.API.DTOs;

public class CvEducationRequest
{
    public string Institution { get; set; } = string.Empty;

    public string Degree { get; set; } = string.Empty;

    public string FieldOfStudy { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }
}