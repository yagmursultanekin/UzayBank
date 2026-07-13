using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UzayBank.Domain.Enums;

namespace UzayBank.Application.DTOs;

public class CreateTransactionDto
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set;  }
    public string Description { get; set; } = string.Empty;
}
