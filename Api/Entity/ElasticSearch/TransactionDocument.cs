using Api.Models;

public class TransactionDocument
{
    public required string PlatformId { get; set; }
    public required float Amount { get; set; }

    public string? Currency { get; set; }

    public string? Description { get; set; }

    public TransactionType TransactionType { get; set; }

    public bool Indexed { get; set; }

    public required string Type { get; set; }
    public DateTime TransactionDate { get; set; }
    public required string TransactionId { get; set; }
    public required string DebitAccountId { get; set; }
    public required string CreditAccountId { get; set; }
    public string? DeviceDocumentId {get;set;}
    public int ObservatoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}