using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NexBank.Domain.Entities;

namespace NexBank.Domain.Interfaces;

public interface IAccountRepository
{
    Task<List<Account>> GetByUserIdAsync (int userId);
    Task<Account?> GetByIdAsync (int accountId);
    Task<List<Transaction>> GetTransactionsAsync(int accountId, DateTime startDate, DateTime endDate);
    Task<bool> IsAccountOwnedByUserAsync(int accountId, int userId);
    Task AddTransactionAsync(Transaction transaction, Account account);

}
