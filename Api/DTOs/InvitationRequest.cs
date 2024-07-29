namespace Api.DTOs
{
    public class InvitationRequest
    {
        public required string UserId { get; set; }
        public required int ObservatoryId { get; set; }
    }

}
