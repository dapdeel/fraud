using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private IModel _channel;
    private string _QueueName;
    private int _sleepTime;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITransferService _transferService;
    public RabbitMqConsumerService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _QueueName = _configuration.GetValue<string>("IngestQueueName");
        _sleepTime = _configuration.GetValue<int>("ConsumerDelayTime");
        _scopeFactory = scopeFactory;
        var connectionFactory = new ConnectionFactory() { HostName = _configuration.GetValue<string>("RabbitMqHost") };
        var connection = connectionFactory.CreateConnection();
        _channel = connection.CreateModel();
        _channel.BasicQos(0, 1, false);
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        _channel.QueueDeclare(queue: _QueueName,
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            Thread.Sleep(_sleepTime);
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine("ConsumerA received: {0}", message);
            var response = JsonSerializer.Deserialize<TransactionTransferRequest>(message);
            if (response != null)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    
                    var transferService = scope.ServiceProvider.GetRequiredService<ITransferService>();
                   await transferService.Ingest(response); // Use scoped service
                }
            }

        };
        _channel.BasicConsume(queue: _QueueName,
                             autoAck: true,
                             consumer: consumer);



        return Task.CompletedTask;
    }
}