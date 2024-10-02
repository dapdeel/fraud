
using Api.Entity;

namespace Api.Services.TransactionTracing
{
    public interface ITransactionTracingService
    {
        Task<int> GetTotalTransactionsLast30Days(string accountNumber);
        Task<decimal> GetTotalAmountLast30Days(string accountNumber);
        Task<int> GetTransactionsLastHour(string accountNumber);
        Task<List<Api.Models.Transaction>> GetAllTransactions(string accountNumber);
        List<TransactionGraphDetails> GetAllTransactions(string ObservatoryId, DateTime dateTime, int pageNumber, int batchSize);
        long GetTransactionCount(string ObservatoryId, DateTime dateTime);
        Task<List<string>> GetSentAccounts(string accountNumber, DateTime startDate, DateTime endDate);
        Task<Api.Models.Transaction> GetTransactionById(int transactionId);
      //  TransactionGraphDetails GetTransactionAsync(int observatoryId, string transactionId);
        TransactionGraphDetails GetAccountNode(int observatoryId, int nodeId);
        List<TransactionTraceResult> GetFutureTransactions(DateTime Date, string AccountNumber, int BankId, string Country);
        public TransactionGraphDetails GetTransactionById(string observatoryId, string transactionId);
        Task<List<Api.Models.Transaction>> GetTransactionsByCustomerId(string customerId);
        public List<WeekDayTransactionCount> GetWeeklyTransactionCounts(string observatoryId);
        public List<HourlyTransactionCount> GetDailyTransactionCounts(string observatoryId, DateTime transactionDate);
        public List<WeeklyTransactionCount> GetMonthlyTransactionCounts(string observatoryId);
    }
}
