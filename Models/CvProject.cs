namespace CVBuilder.API.Models;

public class CvProject
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Technologies { get; set; } = string.Empty;

    public string ProjectUrl { get; set; } = string.Empty;

    public Cv Cv { get; set; } = null!;
}