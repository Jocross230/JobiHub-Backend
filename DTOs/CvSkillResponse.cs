namespace CVBuilder.API.DTOs;

public class CvSkillResponse
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public string Name { get; set; } = string.Empty;
}