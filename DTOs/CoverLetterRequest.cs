namespace CVBuilder.API.DTOs;

public class CoverLetterRequest
{
    public Guid CvId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;
}