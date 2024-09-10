public interface IQueuePublisherService
{
    // public bool PublishAsync(string Queue, string Message);
    public Task<bool> PublishAsync(string queue, string Message);

}