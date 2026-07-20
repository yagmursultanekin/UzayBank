namespace UzayBank.Application.DTOs;

public class TransferDto
{
    public int FromAccountId { get; set; }

    /// <summary>Alıcının IBAN'ı — kullanıcı hesap Id'sini bilemez, IBAN ile transfer yapılır.</summary>
    public string ToIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}