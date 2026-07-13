using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UzayBank.Domain.Enums;

namespace UzayBank.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId {  get; set; }
    public Account Account { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set;  }
    public decimal BalanceAfterTransaction {  get; set; }

}
