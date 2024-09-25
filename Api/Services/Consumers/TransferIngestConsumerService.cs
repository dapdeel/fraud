using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class TransferIngestConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private IModel _channel;
    private string _QueueName;
    private int _sleepTime;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConsumer<Ignore, string> _consumer;
    private CancellationTokenSource _cts;
    public TransferIngestConsumerService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _QueueName = _configuration.GetValue<string>("IngestQueueName");
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
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);
                        var response = JsonSerializer.Deserialize<TransactionTransferRequest>(consumeResult.Message.Value);
                        var transferService = scope.ServiceProvider.GetRequiredService<ITransferService>();
                        if (response != null)
                        {
                            await transferService.Ingest(response, true); // Use scoped service
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }

}