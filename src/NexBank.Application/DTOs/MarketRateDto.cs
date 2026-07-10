namespace NexBank.Application.DTOs;

public class MarketRateDto
{
    public string Code { get; set; } = string.Empty;      // "USD" / "MDNALTIAB1GR995"
    public string Name { get; set; } = string.Empty;      // "Amerikan Doları" / "IAB1Gr Altını"
    public decimal SaleRate { get; set; }
    public decimal PurchaseRate { get; set; }
    public DateTime RateDate { get; set; }
}