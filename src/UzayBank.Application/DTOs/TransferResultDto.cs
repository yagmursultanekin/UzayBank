namespace UzayBank.Application.DTOs;

public class TransferResultDto
{
    public bool Success { get; set; }

    /// <summary>Hata kodu (INSUFFICIENT_FUNDS, RECIPIENT_NOT_FOUND vb.) — frontend çevirir.</summary>
    public string? ErrorCode { get; set; }

    public decimal? NewBalance { get; set; }
}