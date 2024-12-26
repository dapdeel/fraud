namespace Api.DTOs
{
    public class FlaggedAccountDTO
    {
        public int Id { get; set; }
        public string AccountId { get; set; }
        public string AccountNumber { get; set; }
        public float? AccountBalance { get; set; }
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int BankId { get; set; }
        public DateTime FlaggedDate { get; set; }
    }

}
