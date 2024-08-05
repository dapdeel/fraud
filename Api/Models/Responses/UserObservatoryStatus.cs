namespace Api.Models.Responses
{
    public class UserObservatoryStatus
    {
        public int Status { get; set; }
        public List<InvitationResponse>? PendingInvites { get; set; }
    }

}
