namespace Api.Entity
{
    public class WeekDayTransactionCount
    {
        public string Day { get; set; }    
        public long Count { get; set; }   

        public WeekDayTransactionCount(string day, long count)
        {
            Day = day;
            Count = count;
        }
    }

}
