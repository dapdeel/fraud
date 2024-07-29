
using System.ComponentModel.DataAnnotations;

public class ObservatoryRequest
{
    [Required]
    public required string Currency { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Frequency Count must start from One")]
    public int FrequencyCount { get; set; }
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Accepted time in Minutes, Please start from One Minute")]
    public int FrequencyTimer { get; set; }

    [Required]
    public int BankId { get; set; }
    [Required]
    [Range(10, int.MaxValue, ErrorMessage = "At what threshold are you bothered?, Minimum is 10")]
    public float RiskAmount { get; set; }
    public bool UseDefault { get; set; }
    [Required]
    [Range(0, 23, ErrorMessage = "Please specify Odd Hour Start Time")]
    public int OddHourStartTime { get; set; }
    [Required]
    [Range(0, 23, ErrorMessage = "Please specify Odd Hour Stop Time")]
    public int OddHourStopTime { get; set; }
}