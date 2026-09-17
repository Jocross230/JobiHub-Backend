namespace CVBuilder.API.Models;

public class CvSkill
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Cv Cv { get; set; } = null!;
}