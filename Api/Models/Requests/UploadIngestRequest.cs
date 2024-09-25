using System.ComponentModel.DataAnnotations;

public class UploadIngestRequest
{
    [Required]
    public required int ObservatoryId { get; set; }
    public required string ObservatoryTag { get; set; }

}