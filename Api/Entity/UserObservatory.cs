
using System.Text.Json.Serialization;

namespace Api.Models;
public class UserObservatory
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    [JsonIgnore]
    
    public ApplicationUser? User { get; set; } = null;
    public required int ObservatoryId { get; set; }
    public Observatory? Observatory { get; set; } = null;
    public Role Role { get; set; }

    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum Role
{
    Admin,
    Member
}

public enum Status
{
    Member,
    Invited,
    Rejected,
    Removed
}

