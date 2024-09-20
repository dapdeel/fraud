namespace Api.Entity
{
    public class HourlyTransactionCount
    {
        public int Hour { get; set; }  
        public long Count { get; set; }  

        public HourlyTransactionCount(int hour, long count)
        {
            Hour = hour;
            Count = count;
        }
    }

}
