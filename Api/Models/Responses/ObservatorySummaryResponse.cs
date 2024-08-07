namespace Api.Models.Responses;
public class ObservatorySummaryResponse
{
    public double totalAmount { get; set; }
    public double averageAmount { get; set; }
    public int anomaly { get; set; }
    public int suspiciousAccounts { get; set; }
    public int blackListedAccounts { get; set; }
    public int personOfInterest { get; set; }
}