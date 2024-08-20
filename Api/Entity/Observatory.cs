using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Api.Models;
public class Observatory
{
    public int Id { get; set; }
    [Required]
    public required string Currency { get; set; }
    public int FrequencyCount { get; set; }
    public int FrequencyTimer { get; set; }

    public bool Live { get; set; }

    public required string Name { get; set; }
    public float RiskAmount { get; set; }

    public int OddHourStartTime { get; set; }
    public int OddHourStopTime { get; set; }

    public bool IsSetup { get; set; }

    public bool UseDefault { get; set; }

    public string? GraphHost { get; set; }
    public string? GraphDatabase { get; set; }
    public string? GraphUser { get; set; }
    public string? GraphPassword { get; set; }
    public string? ElasticSearchHost { get; set; }
    public ObservatoryType? ObservatoryType { get; set; }
    public int BankId { get; set; }
    public Bank? Bank { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    [JsonIgnore]
    public ICollection<ApplicationUser> Users { get; } = [];
    [JsonIgnore]

    public ICollection<UserObservatory> UserObservatories { get; } = [];

}

public enum ObservatoryType
{
    Swtich,
    Bank

}