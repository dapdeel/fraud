using Api.Models;

public interface ITransferService
{
    public abstract Task<Transaction> Ingest(TransactionTransferRequest request);
}