using Api.Entity;
using Api.Models;

public interface ITransactionTracingGraphService
{
    public TransactionGraphDetails GetTransaction(int ObservatoryId, string transactionId);
    public TransactionGraphDetails GetTransactionAsync(int ObservatoryId, string TransactionId);
    public TransactionGraphDetails NodeDetails(long NodeId);
    public TransactionGraphDetails GetNode(int ObservatoryId, int NodeId);
    public List<TransactionGraphDetails> GetTransactions(int ObservatoryId, DateTime TransactionDate, int page, int batch);
    TransactionGraphDetails GetTransactionById(int observatoryId, string transactionId);
     public long GetTransactionCount(int ObservatoryId, DateTime TransactionDate);
    public List<TransactionTraceResult> Trace(DateTime Date, string AccountNumber, int BankId, string CountryCode);
    public List<WeekDayTransactionCount> GetWeekDayTransactionCounts(int observatoryId);
    public List<HourlyTransactionCount> GetDailyTransactionCounts(int observatoryId);
    public List<WeeklyTransactionCount> GetMonthlyTransactionCounts(int observatoryId);
}