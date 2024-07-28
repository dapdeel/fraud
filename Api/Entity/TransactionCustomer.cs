using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Api.Models;

[Index(nameof(CustomerId), IsUnique = true)]
public class TransactionCustomer
{
    public long Id { get; set; }
    [Required]
    [MaxLength(100)]
    public required string CustomerId { get; set; }
    public string? Email { get; set; }
    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public ICollection<TransactionAccount> TransactionAccounts { get; } = [];

    public ICollection<TransactionProfile> TransactionProfiles { get; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}