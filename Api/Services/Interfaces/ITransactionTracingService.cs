
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
        List<TransactionGraphDetails> GetAllTransactionsWithinDateRange(string observatoryTag, DateTime startDate, DateTime endDate, int pageNumber, int batch);
        long GetTransactionCount(string ObservatoryId, DateTime dateTime);
        long GetTransactionWithinDateRangeCount(string observatoryTag, DateTime startDate, DateTime endDate);
        Task<List<string>> GetSentAccounts(string accountNumber, DateTime startDate, DateTime endDate);
        Task<Api.Models.Transaction> GetTransactionById(int transactionId);
        TransactionGraphDetails GetAccountNode(int observatoryId, int nodeId);
        List<TransactionTraceResult> GetFutureTransactions(DateTime Date, string AccountNumber, int BankId, string Country);
        public TransactionGraphDetails GetTransactionById(string observatoryId, string transactionId);
        Task<List<Api.Models.Transaction>> GetTransactionsByCustomerId(string customerId);
        public List<WeekDayTransactionCount> GetWeeklyTransactionCounts(string observatoryId);
        public List<HourlyTransactionCount> GetDailyTransactionCounts(string observatoryId, DateTime transactionDate);
        public List<WeeklyTransactionCount> GetMonthlyTransactionCounts(string observatoryId);
    }
}
