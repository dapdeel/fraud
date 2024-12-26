namespace Api.DTOs
{
    public class FlaggedTransactionDTO
    {
        public int Id { get; set; }
        public string PlatformId { get; set; }
        public float Amount { get; set; }
        public string? Currency { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionId { get; set; }
        public string DebitAccountId { get; set; }
        public string CreditAccountId { get; set; }
        public string? DeviceDocumentId { get; set; }
        public int ObservatoryId { get; set; }
        public string ObservatoryTag { get; set; }
        public DateTime FlaggedDate { get; set; }   
    }

}
