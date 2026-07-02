using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NexBank.Domain.Enums;

namespace NexBank.Application.DTOs;

public class CreateTransactionDto
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set;  }
    public string Description { get; set; } = string.Empty;
}
