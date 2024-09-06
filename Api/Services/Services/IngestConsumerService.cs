using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using Api.CustomException;


namespace Api.Services.Services
{
    public class RabbitMqIngestConsumerService
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly IServiceScopeFactory _scopeFactory;

        public RabbitMqIngestConsumerService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _connectionFactory = new ConnectionFactory { HostName = configuration.GetValue<string>("RabbitMqHost") };
            _scopeFactory = scopeFactory;
        }

        public void StartConsuming()
        {
            using (var connection = _connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "IngestQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    try
                    {
                        var requestRecord = JsonConvert.DeserializeObject<TransactionTransferRequest>(message);

                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var transferService = scope.ServiceProvider.GetRequiredService<ITransferService>();
                            await transferService.Ingest(requestRecord, false);
                        }
                    }
                    catch (JsonException ex)
                    {
                        throw new ValidateErrorException($"Error deserializing message: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                    }
                };  

                channel.BasicConsume(queue: "IngestQueue", autoAck: true, consumer: consumer);

                Console.WriteLine("Consumer started. Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}
