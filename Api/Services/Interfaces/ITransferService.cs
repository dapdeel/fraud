using Api.DTOs;
using Api.Models;

public interface ITransferService
{
    public abstract Task<TransactionDocument> Ingest(TransactionTransferRequest request, bool IndexToGraph);
    public Task<string> UploadAndIngest(string ObservatoryId, IFormFile file);
    public Task<bool> DownloadFileAndIngest(FileData data);
     public Task<bool> CompleteIngestion();
    Task<FraudAnalysisResult> Analyze(TransactionTransferRequest request);
    
}