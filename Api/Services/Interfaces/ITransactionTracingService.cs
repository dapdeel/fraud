
namespace Api.Services.TransactionTracing
{
    public interface ITransactionTracingService
    {
        Task<int> GetTotalTransactionsLast30Days(string accountNumber);
        Task<decimal> GetTotalAmountLast30Days(string accountNumber);
        Task<int> GetTransactionsLastHour(string accountNumber);
        Task<List<Api.Models.Transaction>> GetAllTransactions(string accountNumber); // Updated
        TransactionGraphDetails GetAllTransactions(int ObservatoryId, DateTime dateTime, int pageNumber, int batchSize);
        long GetTransactionCount(int ObservatoryId, DateTime dateTime);
        Task<List<string>> GetSentAccounts(string accountNumber, DateTime startDate, DateTime endDate);
        Task<Api.Models.Transaction> GetTransactionById(int transactionId); // Updated
        TransactionGraphDetails GetAccountNode(int observatoryId, int nodeId);
        List<TransactionGraphDetails> GetFutureTransactions(DateTime Date, string AccountNumber, string BankCode, string Country);
        TransactionGraphDetails GetTransactionById(int observatoryId, string transactionId);
        Task<List<Api.Models.Transaction>> GetTransactionsByCustomerId(string customerId); // Updated
    }
}
