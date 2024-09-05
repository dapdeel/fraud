using Api.Models.Data;

public interface ITransactionIngestGraphService
{
      public abstract Task<bool> IngestTransactionInGraph(TransactionIngestData data);
      public Task<bool> RunAnalysis(int ObservatoryId);
      public bool IndexPendingTransactions();

}