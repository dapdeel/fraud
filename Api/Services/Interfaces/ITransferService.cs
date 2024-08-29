using Api.Models;

public interface ITransferService
{
    public abstract Task<TransactionDocument> Ingest(TransactionTransferRequest request, bool IndexToGraph);
    public Task<string> UploadAndIngest(int ObservatoryId, IFormFile file);
    public Task<bool> DownloadFileAndIngest(FileData data);
}