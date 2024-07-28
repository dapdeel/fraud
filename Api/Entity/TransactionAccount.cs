using Api.Models;

public class TransactionAccount
{
    public int Id { get; set; }
    public float AccountBalance { get; set; }
    public required string AccountNumber { get; set; }
    public AccountType AccountType { get; set; }
    public int BankId { get; set; }
    public Bank? Bank { get; } = null;
    public long CustomerId { get; set; }
    public TransactionCustomer? TransactionCustomer { get; } = null;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum AccountType
{
    SAVINGS,
    CURRENT,
    UNKNOWN
}