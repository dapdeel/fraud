namespace Api.DTOs
{
    public class TransferredEdgeDocumentDTO  
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

    public class AccountRelationshipResult
    {
        public TransferredEdgeDocumentDTO TransferredDocument { get; set; }
        public double RelationshipScore { get; set; }
        public string RelationshipType { get; set; }  
    }

}
