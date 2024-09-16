namespace Api.DTOs
{
    public class TransactionAccountDto
    {
        public string AccountId { get; set; }
        public string AccountNumber { get; set; }
        public float? AccountBalance { get; set; }
        public string CustomerId { get; set; } // CustomerId as string
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int BankId { get; set; } // Add BankId if it is required
    }


    public class TransactionCustomerDto
    {
        public string CustomerId { get; set; } // CustomerId as string
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AccountWithDetailsDto
    {
        public string AccountId { get; set; }
        public string AccountNumber { get; set; }
        public float? AccountBalance { get; set; }
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
