using System.Text;
using RabbitMQ.Client;

public class RabbitMqQueueService : IQueuePublisherService
{
    private readonly ConnectionFactory _connectionFactory;
    public RabbitMqQueueService(IConfiguration configuration)
    {
        configuration.GetValue<string>("");
        _connectionFactory = new ConnectionFactory { HostName = configuration.GetValue<string>("RabbitMqHost") };
    }
    public async Task<bool> PublishAsync(string queue, string Message)
    {

        using (var connection = _connectionFactory.CreateConnection())
        {
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(Message);
                channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: body);
                return true;
            }
        }
    }
}