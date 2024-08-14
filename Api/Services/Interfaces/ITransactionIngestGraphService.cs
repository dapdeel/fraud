using Api.Models.Data;

public interface ITransactionIngestGraphService {
     public abstract Task<bool> IngestTransactionInGraph(TransactionIngestData data);
}