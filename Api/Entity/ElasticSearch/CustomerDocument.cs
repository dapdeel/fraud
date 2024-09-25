using Api.Models;

public class CustomerDocument
{
    public required string CustomerId { get; set; }

    public int Node { get; set; }
    public string? Email { get; set; }
    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public bool Indexed { get; set; }

    public required string Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string ObservatoryTag {get;set;}
    public required string Document { get; set; }
}
public class AccountWithDetails
{
    public string AccountId { get; set; }
    public string AccountNumber { get; set; }
    public float? AccountBalance { get; set; }
    public string CustomerId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




public class AccountDocument
{
    public required string Document { get; set; }
    public required string AccountId { get; set; }
    public required string AccountNumber { get; set; }
    public int BankId { get; set; }
    public float? AccountBalance { get; set; }
    public bool Indexed { get; set; }
    public int Node { get; set; }
    public required string CustomerId { get; set; }
    public AccountType? AccountType { get; set; }
    public required string Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DeviceDocument
{
    public required string Document { get; set; }
    public string? DeviceId { get; set; }
    public required string ProfileId { get; set; }
    public DeviceType? DeviceType { get; set; }
    public required string CustomerId { get; set; }
    public string? IpAddress { get; set; }
    public string? Longitude { get; set; }
    public required string Type { get; set; }
    public string? Latitude { get; set; }
    public bool Indexed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}

public class OwnsEdgeDocument
{
    public required string Document { get; set; }
    public required string EdgeId { get; set; }
    public required string From { get; set; }
    public required string To { get; set; }
    public required string Type { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransferedEgdeDocument
{
    public required string From { get; set; }
    public required string To { get; set; }
    public required string Document { get; set; }
    public required string EdgeId { get; set; }

    public required double EMEA { get; set; }

    public required DateTime LastTransactionDate { get; set; }
    public float Weight { get; set; }
    public int TransactionCount { get; set; }
    public required string Type { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UsedDeviceEdgeDocument
{

    public required string EdgeId { get; set; }
    public required string From { get; set; }
    public required string To { get; set; }
    public required string Document { get; set; }
    public required string Type { get; set; }
     public DateTime CreatedAt { get; set; }

}
