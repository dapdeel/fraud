
using Api.Models.Responses;

public interface ITransactionSummaryService
{
    public abstract ObservatorySummaryResponse GetObservatorySummary(ObservatorySummaryRequest Request);
}