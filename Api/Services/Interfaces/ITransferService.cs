using Api.Models;

public interface ITransferService
{
    public abstract Task<TransactionDocument> Ingest(TransactionTransferRequest request);
    public Task<bool> UploadAndIngest(IFormFile file);
    public Task<bool> DownloadFileAndIngest(FileData data);
}