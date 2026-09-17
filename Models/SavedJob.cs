namespace CVBuilder.API.Models;

public class SavedJob
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int JobId { get; set; }

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public Job? Job { get; set; }
}