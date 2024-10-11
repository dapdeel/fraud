using Api.Entity;
using Api.Models;

public interface ITransactionTracingGraphService
{
    public TransactionGraphDetails GetTransaction(int ObservatoryId, string transactionId);
    public TransactionGraphDetails GetTransactionAsync(string ObservatoryTag, string TransactionId);
    public TransactionGraphDetails NodeDetails(long NodeId);
    public TransactionGraphDetails GetNode(int ObservatoryId, int NodeId);
    public List<TransactionGraphDetails> GetTransactions(string ObservatoryTag, DateTime TransactionDate, int page, int batch);
    public  List<TransactionGraphDetails> GetTransactionsWithinDateRange(string observatoryTag, DateTime startDate, DateTime endDate, int pageNumber, int batch);
    TransactionGraphDetails GetTransactionById(string observatoryTag, string transactionId);
     public long GetTransactionCount(string ObservatoryTag, DateTime TransactionDate);
    public long GetTransactionWithinDateRangeCount(string observatoryTag, DateTime startDate, DateTime endDate);
    public List<TransactionTraceResult> Trace(DateTime Date, string AccountNumber, int BankId, string CountryCode);
    public List<WeekDayTransactionCount> GetWeekDayTransactionCounts(string observatoryTag);
    public List<HourlyTransactionCount> GetDailyTransactionCounts(string observatoryTag);
    public List<WeeklyTransactionCount> GetMonthlyTransactionCounts(string observatoryTag);
}