using System.ComponentModel.DataAnnotations;
using Api.Models;

public class TransactionTransferRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Please specify an observatory")]
    public required int ObservatoryId { get; set; }

    public string ObservatoryTag { get; set; }


   
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
    public required float Amount { get; set; }

    [Required]
    public required string TransactionId { get; set; }

    [Required]
    public required DateTime TransactionDate { get; set; }

    public string? Currency { get; set; }
    public string? Description { get; set; }

    public string? Country { get; set; }

}

public class CustomerRequest
{
    [Required]
    public required string Email { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required string Phone { get; set; }
    public DeviceRequest? Device { get; set; }
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

public class DeviceRequest
{
    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public DeviceType? DeviceType { get; set; }
}