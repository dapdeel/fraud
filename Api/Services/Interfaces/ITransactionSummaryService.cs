
using Api.Models.Responses;

public interface ITransactionSummaryService
{
    public abstract Task<ObservatorySummaryResponse> GetObservatorySummary(ObservatorySummaryRequest Request);
}