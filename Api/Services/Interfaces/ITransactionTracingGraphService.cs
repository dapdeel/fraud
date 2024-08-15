using Api.Models;

public interface ITransactionTracingGraphService
{
    public TransactionGraphDetails GetTransaction(int ObservatoryId, string transactionId);
    public TransactionGraphDetails NodeDetails(long NodeId);
    public TransactionGraphDetails GetNode(int ObservatoryId, int NodeId);
}