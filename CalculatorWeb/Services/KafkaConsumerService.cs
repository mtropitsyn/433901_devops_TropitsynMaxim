using System.Text.Json;
using CalculatorWeb.Data;
using CalculatorWeb.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CalculatorWeb.Services;

public sealed class KafkaConsumerService(
    IOptions<KafkaOptions> options,
    IServiceScopeFactory scopes,
    CalculatorService calculator,
    ILogger<KafkaConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
                {
                    BootstrapServers = options.Value.BootstrapServers,
                    GroupId = options.Value.GroupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,
                    AllowAutoCreateTopics = false,
                }).Build();
                consumer.Subscribe(options.Value.Topic);
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var message = consumer.Consume(stoppingToken);
                        await ProcessAsync(message, stoppingToken);
                        consumer.Commit(message);
                    }
                }
                finally
                {
                    consumer.Close();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is KafkaException or CalculationStorageException)
            {
                // Rejoin from the last committed offset. The unique request ID makes replay safe.
                logger.LogWarning(exception, "Kafka consumer will retry topic {Topic}.", options.Value.Topic);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(ConsumeResult<string, string> message, CancellationToken cancellationToken)
    {
        CalculationRecord calculation;
        try
        {
            var request = JsonSerializer.Deserialize<CalculationRequest>(message.Message.Value ?? "null");
            if (request is null || request.RequestId == Guid.Empty || message.Message.Key != request.RequestId.ToString("D"))
                throw new JsonException("The request ID is missing or does not match the Kafka key.");

            var result = calculator.Calculate(request.FirstOperand, request.SecondOperand, request.Operation);
            calculation = new CalculationRecord(request.FirstOperand, request.SecondOperand, request.Operation, result, request.RequestId);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or ArithmeticException)
        {
            logger.LogWarning(exception, "Skipped invalid calculation at {Offset}.", message.TopicPartitionOffset);
            return;
        }

        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ICalculationRepository>().SaveAsync(calculation, cancellationToken);
        logger.LogInformation("Calculation {RequestId} saved at {Offset}.", calculation.RequestId, message.TopicPartitionOffset);
    }
}
