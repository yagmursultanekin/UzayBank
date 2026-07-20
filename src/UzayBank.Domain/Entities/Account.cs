using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UzayBank.Domain.Enums;

namespace UzayBank.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string AccountHolderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Bu hesap UzayBank'ın kendi (yapay) hesabı — VakıfBank'tan gelmiyor.
    // İleride farklı hesap türleri eklenirse ayrım için.
    public AccountSource Source { get; set; } = AccountSource.UzayBank;
}
