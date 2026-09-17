namespace CVBuilder.API.Models;

public class Business
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string Industry { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string CompanySize { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}