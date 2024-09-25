namespace Api.Models;

public class TransactionFileDocument
{
    public int Id { get; set; }
    public required string Url { get; set; }
    public required string Name { get; set; }
    public bool Indexed { get; set; }
    public string ObservatoryId { get; set; }
}