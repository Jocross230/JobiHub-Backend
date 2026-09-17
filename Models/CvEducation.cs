namespace CVBuilder.API.Models;

public class CvEducation
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public string Institution { get; set; } = string.Empty;

    public string Degree { get; set; } = string.Empty;

    public string FieldOfStudy { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public Cv Cv { get; set; } = null!;
}