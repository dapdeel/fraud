namespace Api.Models;

public class Bank
{
    public int Id {get;set;}
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string Country { get; set; }
    public ICollection<Observatory> Observatories {get;} = [];
}