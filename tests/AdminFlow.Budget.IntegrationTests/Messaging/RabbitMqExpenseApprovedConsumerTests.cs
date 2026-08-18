using System.Collections.Concurrent;
using System.Text.Json;
using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AdminFlow.Budget.IntegrationTests.Messaging;

[Collection(RabbitMqCollection.Name)]
public sealed class RabbitMqExpenseApprovedConsumerTests
{
    private const string Exchange = "adminflow.budget";
    private const string Queue = "adminflow.budget.expense-approved";
    private const string RoutingKey = "expense.approved";

    [RabbitMqFact]
    public async Task Consume_WhenEventIsValid_ShouldAcknowledgeAndRemoveMessage()
    {
        var (options, factory) = CreateConfiguration();
        await PrepareQueueAsync(factory);
        var logger = new CollectingLogger<ExpenseApprovedConsumer>();
        using var consumer = new ExpenseApprovedConsumer(options, logger);
        await consumer.StartAsync(CancellationToken.None);
        var integrationEvent = new ExpenseApprovedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            "BRL",
            DateTimeOffset.UtcNow);

        await PublishAsync(factory, JsonSerializer.SerializeToUtf8Bytes(integrationEvent));
        await WaitUntilAsync(() => logger.Contains(LogLevel.Information));
        await Task.Delay(100);
        await consumer.StopAsync(CancellationToken.None);

        Assert.Equal((uint)0, await GetMessageCountAsync(factory));
    }

    [RabbitMqFact]
    public async Task Consume_WhenJsonIsInvalid_ShouldRejectWithoutRequeue()
    {
        var (options, factory) = CreateConfiguration();
        await PrepareQueueAsync(factory);
        var logger = new CollectingLogger<ExpenseApprovedConsumer>();
        using var consumer = new ExpenseApprovedConsumer(options, logger);
        await consumer.StartAsync(CancellationToken.None);

        await PublishAsync(factory, "not-json"u8.ToArray());
        await WaitUntilAsync(() => logger.Contains(LogLevel.Warning));
        await Task.Delay(100);
        await consumer.StopAsync(CancellationToken.None);

        Assert.Equal((uint)0, await GetMessageCountAsync(factory));
    }

    private static (RabbitMqOptions Options, ConnectionFactory Factory) CreateConfiguration()
    {
        var options = new RabbitMqOptions
        {
            Enabled = true,
            HostName = "localhost",
            Port = 5672,
            UserName = "adminflow",
            Password = Environment.GetEnvironmentVariable(
                "ADMINFLOW_TEST_RABBITMQ_PASSWORD")!
        };

        return (options, new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password
        });
    }

    private static async Task PrepareQueueAsync(ConnectionFactory factory)
    {
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, durable: true);
        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(Queue, Exchange, RoutingKey);
        await channel.QueuePurgeAsync(Queue);
    }

    private static async Task PublishAsync(ConnectionFactory factory, byte[] body)
    {
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.BasicPublishAsync(Exchange, RoutingKey, body);
    }

    private static async Task<uint> GetMessageCountAsync(ConnectionFactory factory)
    {
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var result = await channel.QueueDeclarePassiveAsync(Queue);
        return result.MessageCount;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The RabbitMQ consumer did not process the message.");
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        private readonly ConcurrentQueue<LogLevel> _levels = new();

        public bool Contains(LogLevel level) => _levels.Contains(level);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _levels.Enqueue(logLevel);
        }
    }
}
