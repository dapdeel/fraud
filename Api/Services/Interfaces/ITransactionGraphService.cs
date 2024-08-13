using Api.Models.Data;

public interface ITransactionGraphService {
     public abstract Task<bool> IngestTransactionInGraph(TransactionIngestData data);
}