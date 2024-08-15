using Api.Models;

public interface ITransferService
{
    public abstract Task<Transaction> Ingest(TransactionTransferRequest request);
     public  Task<bool> UploadAndIngest(IFormFile file);
}