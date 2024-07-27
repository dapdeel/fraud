
namespace Api.Models;
public class UserObservatory
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required ApplicationUser User { get; set; }
    public int ObservatoryId { get; set; }
    public required Observatory Observatory { get; set; }
    public Role Role { get; set; }
}

public enum Role
{
    Admin,
    Member
}