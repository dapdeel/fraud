public class BankRequest
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? ImageUrl { get; set; }
    public string Country {get;set;} = "NGN";
}