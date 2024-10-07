public class TransactionListRequest
{
    public string ObservatoryId { get; set; }
    public DateTime dateTime { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int pageNumber { get; set; }
    public int batchSize { get; set; }
}