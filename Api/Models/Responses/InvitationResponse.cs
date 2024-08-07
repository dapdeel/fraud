namespace Api.Models.Responses
{
    public class InvitationResponse
    {
        public int ObservatoryId { get; set; }
        public string ObservatoryName { get; set; }
        public DateTime InvitedAt { get; set; }
    }
}
