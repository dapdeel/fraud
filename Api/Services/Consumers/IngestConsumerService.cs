using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using Api.CustomException;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace Api.Services.Consumers
{
    public class IngestConsumerService
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<IngestConsumerService> _logger;
        private readonly IConfiguration _configuration;

        public IngestConsumerService(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<IngestConsumerService> logger)
        {
            _connectionFactory = new ConnectionFactory { HostName = configuration.GetValue<string>("RabbitMqHost") };
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public void StartIngestConsuming()
        {
           
            // using (var connection = _connectionFactory.CreateConnection())
            // using (var channel = connection.CreateModel())
            // {
            //     var queueName = _configuration.GetValue<string>("IngestQueueName");
            //     if (string.IsNullOrEmpty(queueName))
            //     {
            //         throw new InvalidOperationException("Ingest queue name is not configured.");
            //     }

            //     channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            //     var consumer = new EventingBasicConsumer(channel);
            //     consumer.Received += async (model, ea) =>
            //     {
            //         var body = ea.Body.ToArray();
            //         var message = Encoding.UTF8.GetString(body);

            //         try
            //         {
            //             _logger.LogInformation("Received a message from the queue.");
            //             _logger.LogInformation($"Message content: {message}");

            //             var data = JsonConvert.DeserializeObject<TransactionTransferRequest>(message);

                    

            //             using (var scope = _scopeFactory.CreateScope())
            //             {
            //                 var transferService = scope.ServiceProvider.GetRequiredService<ITransferService>();
            //                 if(data == null)
            //                 {
            //                     SentrySdk.CaptureMessage("Invalid File");
            //                     _logger.LogWarning("File data processing failed.");
            //                 }
            //                 var document = await transferService.Ingest(data,true);

            //                 if (document == null)
            //                 {
            //                     _logger.LogWarning("File data processing failed.");
                              
            //                 }
            //             }

 
            //             channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            //             _logger.LogInformation("Message processed and acknowledged successfully.");
            //         }
            //         catch (JsonException ex)
            //         {
            //             _logger.LogError($"Error deserializing message: {ex.Message}");
            //             channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            //         }
            //         catch (ValidateErrorException ex)
            //         {
            //             _logger.LogError($"Validation error: {ex.Message}");
            //             channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); 
            //         }
            //         catch (Exception ex)
            //         {
            //             _logger.LogError($"Error processing message: {ex.Message}");
            //             channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            //         }
            //     };

            //     channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            //     _logger.LogInformation("Consumer started. Press [enter] to exit.");
            //     Console.ReadLine();
            // }
        
        
        }
    }
}
