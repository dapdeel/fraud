public interface IQueuePublisherService
{
    public bool Publish(string Queue, string Message);

}