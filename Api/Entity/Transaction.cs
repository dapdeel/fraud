namespace Api.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public class Transaction
{
    public int Id { get; set; }
    public required string PlatformId { get; set; }
    public required float Amount { get; set; }

    public string? Currency { get; set; }

    public string? Description { get; set; }

    public bool Indexed { get; set; }

    public DateTime TransactionDate { get; set; }
    public required string TransactionId { get; set; }

    public TransactionType TransactionType { get; set; }

    public int DebitAccountId { get; set; }

    public TransactionAccount? DebitAccount { get; }

    public int CreditAccountId { get; set; }

    public TransactionAccount? CreditAccount { get; }

    public required int ObservatoryId { get; set; }
    public Observatory? Observatory { get; } = null;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}

public enum TransactionType
{
    Withdrawal,
    Transfer,
    Bills,
    Unknown

}