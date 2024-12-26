using Api.Entity;

namespace Api.Services.Interfaces
{
    public interface IFlaggingService
    {
        Task<bool> MarkTransactionAsSuspicious(string observatoryTag, string transactionId);
        Task<bool> MarkTransactionAsBlacklisted(string observatoryTag, string transactionId);
        Task<bool> MarkAccountAsSuspicious(string accountNumber, int bankId);
        Task<bool> MarkAccountAsBlacklistedAsync(string accountNumber, int bankId);
        List<BlacklistedTransaction> GetBlacklistedTransactions(string observatoryTag);
        List<SuspiciousTransaction> GetSuspiciousTransactions(string observatoryTag);
        List<BlacklistedAccount> GetBlacklistedAccounts(string observatoryTag);
        List<SuspiciousAccount> GetSuspiciousAccounts(string observatoryTag);
    }
}
