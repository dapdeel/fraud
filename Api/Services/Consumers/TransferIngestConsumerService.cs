using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class TransferIngestConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private IModel _channel;
    private string _QueueName;
    private int _sleepTime;
    private readonly IServiceScopeFactory _scopeFactory;
    public TransferIngestConsumerService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _QueueName = _configuration.GetValue<string>("IngestQueueName");
        _sleepTime = _configuration.GetValue<int>("ConsumerDelayTime");
        _scopeFactory = scopeFactory;
        var HostName = _configuration.GetValue<string>("RabbitMqHost") ;
        var connectionFactory = new ConnectionFactory() { HostName = HostName };
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
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var response = JsonSerializer.Deserialize<TransactionTransferRequest>(message);
            if (response != null)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                var transferService = scope.ServiceProvider.GetRequiredService<ITransferService>();
                   await transferService.Ingest(response, true); // Use scoped service
                }
            }

        };
        _channel.BasicConsume(queue: _QueueName,
                             autoAck: true,
                             consumer: consumer);



        return Task.CompletedTask;
    }
}