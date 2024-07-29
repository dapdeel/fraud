using System.ComponentModel.DataAnnotations;
using Api.Models;

public class TransactionTransferRequest
{
    [Required]
    public int ObservatoryId;
    [Required]
    public required CustomerRequest DebitCustomer { get; set; }
    [Required]
    public required CustomerRequest CreditCustomer { get; set; }
    [Required]
    public required TransactionRequest Transaction { get; set; }
}

public class TransactionRequest
{
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "The Transaction Amount cannot be Negative")]
    public required float Amount;
    [Required]
    public required string TransactionId;
    [Required]
    public required DateTime TransactionDate;
    public string? Description;
    public string? Country;
}

public class CustomerRequest
{
    [Required]
    public required string Email { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required string Phone { get; set; }
    public ProfileRequest? Profile { get; set; }
    [Required]
    public required AccountRequest Account { get; set; }

}

public class AccountRequest
{
    [Required]
    public required string AccountNumber { get; set; }

    public AccountType? AccountType { get; set; }
    public float? Balance { get; set; }
    [Required]
    public required string BankCode { get; set; }
    public required string Country { get; set; }
}

public class ProfileRequest
{
    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public DeviceType? DeviceType { get; set; }
}