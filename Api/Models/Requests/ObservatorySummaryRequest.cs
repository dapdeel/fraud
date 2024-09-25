public class ObservatorySummaryRequest
{
    public int ObservatoryId { get; set; }

    public string ObservatoryTag { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}