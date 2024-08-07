
using System.ComponentModel.DataAnnotations;
using Api.CustomException;
using Api.Models.Responses;
using Api.Services.Interfaces;
using Gremlin.Net.Process.Traversal;

public class TransactionSummaryService : ITransactionSummaryService
{
    IGraphService _graphService;
    public TransactionSummaryService(IGraphService graphService)
    {
        _graphService = graphService;
    }
    public async Task<ObservatorySummaryResponse> GetObservatorySummary(ObservatorySummaryRequest request)
    {
        var connector = _graphService.connect(request.ObservatoryId);
        var g = connector.traversal();
        try
        {
            var avgAmount = g.V().HasLabel(JanusService.TransactionNode)
                         .Has("TransactionDate", P.Gte(request.StartDate))
                        .Has("TransactionDate", P.Lte(request.EndDate))
                           .Values<double>("Amount")
                          .Mean<double>()
                           .Next();
            var totalAmount = g.V().HasLabel(JanusService.TransactionNode)
                           .Has("TransactionDate", P.Gte(request.StartDate))
                           .Has("TransactionDate", P.Lte(request.EndDate))
                           .Values<double>("Amount")
                           .Sum<double>()
                           .Next();
            var response = new ObservatorySummaryResponse
            {
                anomaly = 0,
                averageAmount = avgAmount,
                blackListedAccounts = 0,
                personOfInterest = 0,
                suspiciousAccounts = 0,
                totalAmount = totalAmount
            };
            return response;

        }
        catch (Exception exception)
        {
            throw new ValidateErrorException("There issues in fetching summary, please try again");
        }
        finally
        {
            connector.Client().Dispose();
        }

    }
}