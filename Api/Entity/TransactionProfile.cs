namespace Api.Models;
public class TransactionProfile
{
    public long Id { get; set; }
    public required string DeviceId { get; set; }
    public required string ProfileId {get;set;}
    public DeviceType? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public string? Longitude { get; set; }
    public string? Latitude { get; set; }
    public required long CustomerId { get; set; }
    public TransactionCustomer? TransactionCustomer { get; } = null;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}
public enum DeviceType
{
    ANDROID,
    WEB,
    MOBILE_WEB,
    IOS,
    DESKTOP,
    UNKNOWN
}