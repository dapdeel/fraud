public class TransactionListRequest
{
    public string ObservatoryId { get; set; }
    public DateTime dateTime { get; set; }
    public int pageNumber { get; set; }
    public int batchSize { get; set; }
}