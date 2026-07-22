using AutoMapper;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using UzayBank.Domain.Enums;
using UzayBank.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UzayBank.Domain.Entities;

namespace UzayBank.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;

    public AccountService(IAccountRepository accountRepository, IMapper mapper)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
    }

    public async Task<List<AccountDto>> GetAccountsByUserIdAsync(int userId)
    {
        var accounts = await _accountRepository.GetByUserIdAsync(userId);
        return _mapper.Map<List<AccountDto>>(accounts);
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        return account == null ? null : _mapper.Map<AccountDto>(account);
    }

    public async Task<List<TransactionDto>> GetTransactionsByAccountIdAsync(int accountId, DateTime startDate, DateTime endDate)
    {
        var transactions = await _accountRepository.GetTransactionsAsync(accountId, startDate, endDate);
        return _mapper.Map<List<TransactionDto>>(transactions);
    }

    public async Task<bool> IsAccountOwnedByUserAsync(int accountId, int userId)
    {
        return await _accountRepository.IsAccountOwnedByUserAsync(accountId, userId);
    }

    public async Task<TransactionDto?> AddTransactionAsync(int accountId, int userId, CreateTransactionDto dto)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account == null || account.UserId != userId)
            return null;

        // Bakiyeyi güncelle
        if (dto.Type == TransactionType.Credit)
            account.Balance += dto.Amount;
        else
            account.Balance -= dto.Amount;

        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = dto.Amount,
            Type = dto.Type,
            Description = dto.Description,
            TransactionDate = DateTime.UtcNow,
            BalanceAfterTransaction = account.Balance
        };

        await _accountRepository.AddTransactionAsync(transaction, account);

        return _mapper.Map<TransactionDto>(transaction);
    }
    /// <summary>
    /// Kullanıcının tüm hesaplarındaki işlemleri birleştirir (MSSQL kaynağı).
    /// </summary>
    public async Task<List<TransactionDto>> GetAllTransactionsByUserIdAsync(
        int userId, DateTime startDate, DateTime endDate)
    {
        var accounts = await GetAccountsByUserIdAsync(userId);

        var all = new List<TransactionDto>();
        foreach (var account in accounts)
        {
            var txs = await GetTransactionsByAccountIdAsync(account.Id, startDate, endDate);
            all.AddRange(txs);
        }

        return all.OrderByDescending(t => t.TransactionDate).ToList();
    }
    /// <summary>
    /// Yönetici paneli için tüm hesapları döndürür (MSSQL kaynağı).
    ///
    /// Kullanıcı filtresi YOK — yönetici, henüz kimseye atanmamış hesapları da
    /// görmek zorunda, çünkü atama işlemini o yapıyor.
    ///
    /// Önceki hali GetAccountsByUserIdAsync(0) çağırıyordu; sistemde 0 numaralı
    /// kullanıcı olmadığı için bu her zaman boş liste döndürüyordu ve MSSQL
    /// modunda yönetici paneli çalışmıyordu.
    /// </summary>
    public async Task<List<AccountDto>> GetAllAccountsForAdminAsync()
    {
        var accounts = await _accountRepository.GetAllAsync();
        return _mapper.Map<List<AccountDto>>(accounts);
    }

}
