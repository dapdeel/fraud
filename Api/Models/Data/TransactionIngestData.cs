namespace Api.Models.Data;
public class TransactionIngestData
{
    public required TransactionAccount DebitAccount { get; set; }
    public required TransactionAccount CreditAccount { get; set; }

    public required TransactionCustomer DebitCustomer { get; set; }
    public required TransactionCustomer CreditCustomer { get; set; }
    public required Transaction Transaction { get; set; }
    public TransactionProfile? TransactionProfile { get; set; }
}