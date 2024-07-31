public class BankRequest
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? ImageUrl { get; set; }
    public required string Country {get;set;}
}