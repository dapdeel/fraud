using System.Text;
using RabbitMQ.Client;
using Confluent.Kafka;
using Api.CustomException;

public class KafkaProducerService : IQueuePublisherService
{
    private ConnectionFactory? _connectionFactory;
    private ProducerConfig _producerConfig;
    public KafkaProducerService(IConfiguration configuration)
    {
        _producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration.GetValue<string>("KafkaServer"),
            Acks = Acks.All
        };
    }
    public async Task<bool> PublishAsync(string queue, string Message)
    {
        using (var producer = new ProducerBuilder<Null, string>(_producerConfig).Build())
        {
            try
            {
                var result = await producer.ProduceAsync(queue, new Message<Null, string> { Value = Message });
                Console.WriteLine($"Produced message to: {result.TopicPartitionOffset}");
                return true;
            }
            catch (ProduceException<Null, string> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                throw new ValidateErrorException("Unable to Enqueu Message");
            }
        }
    }
}