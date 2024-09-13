using System.ComponentModel.DataAnnotations;

public class UploadIngestRequest
{
    [Required]
    public required int ObservatoryId { get; set; }

}