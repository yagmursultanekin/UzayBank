using UzayBank.Application.DTOs;

namespace UzayBank.Application.Interfaces;

public interface IUzayAccountService
{
    /// <summary>Kullanıcının UzayBank (yapay) hesapları.</summary>
    Task<List<AccountDto>> GetMyAccountsAsync(int userId);

    /// <summary>Yeni UzayBank hesabı açar. Hesap no ve IBAN otomatik üretilir.</summary>
    Task<AccountDto> CreateAccountAsync(int userId, CreateUzayAccountDto dto);

    /// <summary>Hesabın işlem geçmişi. Sahiplik kontrolü içerir.</summary>
    Task<List<TransactionDto>> GetTransactionsAsync(int accountId, int userId);

    /// <summary>
    /// Hesaplar arası transfer. Gönderen ve alıcı hesaplarda ayrı işlem kaydı oluşur,
    /// tamamı tek veritabanı transaction'ında yürütülür.
    /// </summary>
    Task<TransferResultDto> TransferAsync(int userId, TransferDto dto);
   
    /// <summary>Hesaba para yatırır.</summary>
    Task<TransferResultDto> DepositAsync(int userId, DepositWithdrawDto dto);

    /// <summary>Hesaptan para çeker. Bakiye yetersizse başarısız döner.</summary>
    Task<TransferResultDto> WithdrawAsync(int userId, DepositWithdrawDto dto);
}