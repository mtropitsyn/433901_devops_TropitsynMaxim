using System.Text.Json;
using CalculatorWeb.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CalculatorWeb.Services;

public interface ICalculationPublisher
{
    Task PublishAsync(CalculationRequest request, CancellationToken cancellationToken);
}

public sealed class KafkaProducerService(IOptions<KafkaOptions> options) : ICalculationPublisher, IDisposable
{
    private readonly IProducer<string, string> producer = new ProducerBuilder<string, string>(new ProducerConfig
    {
        BootstrapServers = options.Value.BootstrapServers,
        EnableIdempotence = true,
        MessageTimeoutMs = 8000,
        AllowAutoCreateTopics = false,
    }).Build();

    public async Task PublishAsync(CalculationRequest request, CancellationToken cancellationToken)
    {
        await producer.ProduceAsync(options.Value.Topic, new Message<string, string>
        {
            Key = request.RequestId.ToString("D"),
            Value = JsonSerializer.Serialize(request),
        }, cancellationToken);
    }

    public void Dispose() => producer.Dispose();
}
