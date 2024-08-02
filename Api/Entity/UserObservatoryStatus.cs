namespace Api.Entity
{
    public class UserObservatoryStatus
    {
        public bool IsMember { get; set; }
        public bool IsInvited { get; set; }
        public List<InvitationResponse> PendingInvites { get; set; }
    }
}
