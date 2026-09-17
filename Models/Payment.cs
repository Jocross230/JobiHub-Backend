namespace CVBuilder.API.Models;

public class Payment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    // PremiumCV, CoverLetterPack, or JobReady
    public string Product { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    // TransferXO transaction/payment reference
    public string PaymentReference { get; set; } = string.Empty;

    // Pending, Approved, Rejected
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public User? User { get; set; }
}