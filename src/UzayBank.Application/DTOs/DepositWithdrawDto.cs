namespace UzayBank.Application.DTOs;

public class DepositWithdrawDto
{
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}