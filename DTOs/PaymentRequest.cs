namespace CVBuilder.API.DTOs;

public class PaymentRequest
{
    public string Product { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string PaymentReference { get; set; } = string.Empty;
}