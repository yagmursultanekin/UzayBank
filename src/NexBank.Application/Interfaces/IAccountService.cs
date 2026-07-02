using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NexBank.Application.DTOs;

namespace NexBank.Application.Interfaces;

public interface IAccountService
{
    Task<List<AccountDto>> GetAccountsByUserIdAsync(int userId);
    Task<AccountDto?> GetAccountByIdAsync(int accountId);
    Task<List<TransactionDto>> GetTransactionsByAccountIdAsync(int accountId, DateTime startDate, DateTime endDate);
    Task<bool> IsAccountOwnedByUserAsync(int accountId, int userId);
    Task<TransactionDto?> AddTransactionAsync(int accountId, int userId, CreateTransactionDto dto);
}
