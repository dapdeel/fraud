namespace Api.Entity
{
    public class AccountListRequest
    {
        public int PageNumber { get; set; }
        public int BatchSize { get; set; }

        public string ObservatoryTag { get; set; }
    }

}
