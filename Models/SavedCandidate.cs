namespace CVBuilder.API.Models;

public class SavedCandidate
{
    public int Id { get; set; }

    public int BusinessId { get; set; }

    public Guid CvId { get; set; }

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    public Business? Business { get; set; }

    public Cv? Cv { get; set; }
}