using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class IngestConsumer
{
    private readonly IModel _channel;
    private readonly IConfiguration _configuration;
    private readonly ITransferService _transferService;
    public IngestConsumer(IModel channel, IConfiguration configuration, ITransferService transferService)
    {
        _channel = channel;
        _configuration = configuration;
        _transferService = transferService;

    }

    public async Task StartConsuming()
    {
        var queueName = _configuration.GetValue<string>("IngestQueueName");

        _channel.QueueDeclare(queue: queueName,
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += static async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Thread.Sleep(1000);
            Console.WriteLine("ConsumerA received: {0}", message);
            TransactionTransferRequest request = JsonSerializer.Deserialize<TransactionTransferRequest>(message);
           // _transferService.Ingest(request);
        //    await CallExternalService(message);
            
            // Handle the message

        };
        _channel.BasicConsume(queue: queueName,
                             autoAck: true,
                             consumer: consumer);


    }
      private async Task CallExternalService(string message){

      }
}