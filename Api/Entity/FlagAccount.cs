namespace Api.Entity
{
    public class FlagAccount
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string FlagType { get; set; } 
        public DateTime FlaggedAt { get; set; }
    }
}
