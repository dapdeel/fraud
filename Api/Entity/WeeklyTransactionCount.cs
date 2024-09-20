namespace Api.Entity
{
    public class WeeklyTransactionCount
    {
        public int WeekNumber { get; set; }  
        public long TransactionCount { get; set; }  

        public WeeklyTransactionCount(int weekNumber, long transactionCount)
        {
            WeekNumber = weekNumber;
            TransactionCount = transactionCount;
        }
    }

}
