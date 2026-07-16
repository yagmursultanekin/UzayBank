namespace UzayBank.Application.DTOs;

public class AccountAssignmentDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;

    // null ise hesap henüz kimseye atanmamış
    public int? AssignedUserId { get; set; }
    public string? AssignedUserEmail { get; set; }
}