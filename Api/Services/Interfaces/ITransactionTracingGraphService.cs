using Api.Models;

public interface ITransactionTracingGraphService
{
    public TransactionGraphDetails GetTransaction(int ObservatoryId, string transactionId);
    public TransactionGraphDetails NodeDetails(long NodeId);
    public TransactionGraphDetails GetNode(int ObservatoryId, int NodeId);
    public TransactionGraphDetails GetTransactions(int ObservatoryId, DateTime TransactionDate, int page, int batch);
     public long GetTransactionCount(int ObservatoryId, DateTime TransactionDate);
    public List<TransactionGraphDetails> Trace(DateTime Date, string AccountNumber, string BankCode, string CountryCode);
}