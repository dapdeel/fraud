namespace Api.Models.Data;
public class TransactionIngestData
{
    public required AccountDocument DebitAccount { get; set; }
    public required AccountDocument CreditAccount { get; set; }

    public required CustomerDocument DebitCustomer { get; set; }
    public required CustomerDocument CreditCustomer { get; set; }
    public required TransactionDocument Transaction { get; set; }
    public int ObservatoryId {get;set;}
    public DeviceDocument? Device { get; set; }
}