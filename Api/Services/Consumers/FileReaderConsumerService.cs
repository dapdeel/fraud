
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class FileReaderConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private IModel _channel;
    private string _QueueName;
    private IConsumer<Ignore, string> _consumer;
    private int _sleepTime;
    private readonly IServiceScopeFactory _scopeFactory;
    public FileReaderConsumerService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _QueueName = _configuration.GetValue<string>("IngestFileQueueName");
        _scopeFactory = scopeFactory;
        var HostName = _configuration.GetValue<string>("KafkaServer");
        var config = new ConsumerConfig
        {
            BootstrapServers = HostName,
            GroupId = _QueueName,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _consumer.Subscribe(_QueueName);
    }
    private IConnection connect()
    {
        var HostName = _configuration.GetValue<string>("RabbitMqHost");
        var connectionFactory = new ConnectionFactory() { HostName = HostName };
        var connection = connectionFactory.CreateConnection();
        _channel = connection.CreateModel();
        _channel.BasicQos(0, 1, false);
        return connection;
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsuming(stoppingToken), stoppingToken);
    }
    private async void StartConsuming(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    var response = JsonSerializer.Deserialize<FileData>(consumeResult.Message.Value);
                    if (response != null)
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var transferService = scope.ServiceProvider.GetRequiredService<ITransferService>();
                            await transferService.DownloadFileAndIngest(response); // Use scoped service
                        }
                    }
                }

                catch (ConsumeException ex)
                {
                    // _logger.LogError($"Error occurred: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException Exception)
        {
            Console.WriteLine(Exception.Message);
        }
        finally
        {
            _consumer.Close(); // Cleanly close the consumer and commit offsets
        }
    }
}