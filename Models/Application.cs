namespace CVBuilder.API.Models;

public class Application
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int UserId { get; set; }

    public Guid CvId { get; set; }

    public string Status { get; set; } = "submitted";

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public Job? Job { get; set; }

    public Cv? Cv { get; set; }

    public User? User { get; set; }
}