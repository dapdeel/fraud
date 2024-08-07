
namespace Api.Services.TransactionTracing
{
    public interface ITransactionTracingService
    {
        Task<int> GetTotalTransactionsLast30Days(string accountNumber);
        Task<decimal> GetTotalAmountLast30Days(string accountNumber);
        Task<int> GetTransactionsLastHour(string accountNumber);
        Task<List<Api.Models.Transaction>> GetAllTransactions(string accountNumber); // Updated
        Task<List<string>> GetSentAccounts(string accountNumber, DateTime startDate, DateTime endDate);
        Task<Api.Models.Transaction> GetTransactionById(int transactionId); // Updated
        Task<List<Api.Models.Transaction>> GetTransactionsByCustomerId(string customerId); // Updated
    }
}
