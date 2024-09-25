using Api.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Entity
{
    public class TransactionRules
    {
        public int Id { get; set; }

        [Required]
        public int ObservatoryId { get; set; }

        public int AlertFrequencyMinutes { get; set; } = 30;
        public float RiskAppetiteAmount { get; set; } = 100000;
        public bool AllowSuspiciousAccounts { get; set; } = true;
        public bool BlockFraudulentAccounts { get; set; } = false;
        public bool AlertFraudulentAccounts { get; set; } = true;
        public bool AlertHighRiskTransactions { get; set; } = true;


        [JsonIgnore]
        public Observatory? Observatory { get; set; } 
    }

}
