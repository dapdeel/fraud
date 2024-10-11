namespace Api.Entity
{
    public class FlagTransaction
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string FlagType { get; set; }
        public DateTime FlaggedAt { get; set; }
    }
}
