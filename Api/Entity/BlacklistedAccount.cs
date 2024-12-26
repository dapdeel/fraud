namespace Api.Entity
{
    public class BlacklistedAccount
    {
        public int Id { get; set; }
        public string AccountId { get; set; }
        public string ObservatoryTag { get; set; }
        public string AccountNumber { get; set; }
        public int BankId { get; set; }
        public float? AccountBalance { get; set; }
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime FlaggedDate { get; set; }
        
    }
}



