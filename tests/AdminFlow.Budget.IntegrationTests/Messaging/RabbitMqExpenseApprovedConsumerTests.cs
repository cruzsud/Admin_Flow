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
    private const string RetryExchange = "adminflow.budget.retry";
    private const string RetryQueue = "adminflow.budget.expense-approved.retry";
    private const string RetryRoutingKey = "expense.approved.retry";
    private const string DeadLetterExchange = "adminflow.budget.dead-letter";
    private const string DeadLetterQueue = "adminflow.budget.expense-approved.dead-letter";
    private const string DeadLetterRoutingKey = "expense.approved.dead-letter";

    [RabbitMqFact]
    public async Task Consume_WhenEventIsValid_ShouldAcknowledgeAndRemoveMessage()
    {
        var (options, factory) = CreateConfiguration();
        await PrepareQueueAsync(factory);
        var logger = new CollectingLogger<ExpenseApprovedConsumer>();
        var handler = new RecordingHandler();
        using var consumer = new ExpenseApprovedConsumer(options, logger, handler);
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
        await WaitUntilAsync(() => handler.CallCount == 1);
        await Task.Delay(100);
        await consumer.StopAsync(CancellationToken.None);

        Assert.Equal((uint)0, await GetMessageCountAsync(factory));
    }

    [RabbitMqFact]
    public async Task Consume_WhenJsonIsInvalid_ShouldMoveToDeadLetterQueue()
    {
        var (options, factory) = CreateConfiguration();
        await PrepareQueueAsync(factory);
        var logger = new CollectingLogger<ExpenseApprovedConsumer>();
        using var consumer = new ExpenseApprovedConsumer(
            options,
            logger,
            new RecordingHandler());
        await consumer.StartAsync(CancellationToken.None);

        await PublishAsync(factory, "not-json"u8.ToArray());
        await WaitUntilAsync(() => logger.Contains(LogLevel.Warning));
        await Task.Delay(100);
        await consumer.StopAsync(CancellationToken.None);

        Assert.Equal((uint)0, await GetMessageCountAsync(factory));
        Assert.Equal((uint)1, await GetMessageCountAsync(factory, DeadLetterQueue));
    }

    [RabbitMqFact]
    public async Task Consume_WhenHandlerFailsOnce_ShouldRetryAndThenAcknowledge()
    {
        var (options, factory) = CreateConfiguration();
        await PrepareQueueAsync(factory);
        var handler = new RecordingHandler(failuresBeforeSuccess: 1);
        using var consumer = new ExpenseApprovedConsumer(
            options,
            new CollectingLogger<ExpenseApprovedConsumer>(),
            handler);
        await consumer.StartAsync(CancellationToken.None);

        await PublishAsync(factory, CreateValidBody());
        await WaitUntilAsync(() => handler.CallCount == 2);
        await Task.Delay(200);
        await consumer.StopAsync(CancellationToken.None);

        Assert.Equal((uint)0, await GetMessageCountAsync(factory));
        Assert.Equal((uint)0, await GetMessageCountAsync(factory, RetryQueue));
        Assert.Equal((uint)0, await GetMessageCountAsync(factory, DeadLetterQueue));
    }

    [RabbitMqFact]
    public async Task Consume_WhenRetryLimitIsExceeded_ShouldMoveToDeadLetterQueue()
    {
        var (options, factory) = CreateConfiguration();
        await PrepareQueueAsync(factory);
        var handler = new RecordingHandler(failuresBeforeSuccess: int.MaxValue);
        using var consumer = new ExpenseApprovedConsumer(
            options,
            new CollectingLogger<ExpenseApprovedConsumer>(),
            handler);
        await consumer.StartAsync(CancellationToken.None);

        await PublishAsync(factory, CreateValidBody());
        await WaitUntilAsync(async () =>
            await GetMessageCountAsync(factory, DeadLetterQueue) == 1);
        await consumer.StopAsync(CancellationToken.None);

        Assert.Equal(options.MaxRetryAttempts + 1, handler.CallCount);
        Assert.Equal((uint)0, await GetMessageCountAsync(factory));
        Assert.Equal((uint)0, await GetMessageCountAsync(factory, RetryQueue));
        Assert.Equal((uint)1, await GetMessageCountAsync(factory, DeadLetterQueue));
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
                "ADMINFLOW_TEST_RABBITMQ_PASSWORD")!,
            MaxRetryAttempts = 2,
            RetryDelayMilliseconds = 100
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
        await channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Direct, durable: true);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true);
        await channel.QueueDeclareAsync(
            Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RetryExchange,
                ["x-dead-letter-routing-key"] = RetryRoutingKey
            });
        await channel.QueueBindAsync(Queue, Exchange, RoutingKey);
        await channel.QueueDeclareAsync(
            RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = 100,
                ["x-dead-letter-exchange"] = Exchange,
                ["x-dead-letter-routing-key"] = RoutingKey
            });
        await channel.QueueBindAsync(RetryQueue, RetryExchange, RetryRoutingKey);
        await channel.QueueDeclareAsync(
            DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        await channel.QueueBindAsync(
            DeadLetterQueue,
            DeadLetterExchange,
            DeadLetterRoutingKey);
        await channel.QueuePurgeAsync(Queue);
        await channel.QueuePurgeAsync(RetryQueue);
        await channel.QueuePurgeAsync(DeadLetterQueue);
    }

    private static async Task PublishAsync(ConnectionFactory factory, byte[] body)
    {
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.BasicPublishAsync(Exchange, RoutingKey, body);
    }

    private static async Task<uint> GetMessageCountAsync(
        ConnectionFactory factory,
        string queue = Queue)
    {
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var result = await channel.QueueDeclarePassiveAsync(queue);
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

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The RabbitMQ condition was not reached.");
    }

    private static byte[] CreateValidBody()
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            new ExpenseApprovedIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                100m,
                "BRL",
                DateTimeOffset.UtcNow));
    }

    private sealed class RecordingHandler(int failuresBeforeSuccess = 0)
        : IExpenseApprovedIntegrationEventHandler
    {
        private int _callCount;

        public int CallCount => _callCount;

        public Task HandleAsync(
            ExpenseApprovedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken)
        {
            var currentCall = Interlocked.Increment(ref _callCount);
            if (currentCall <= failuresBeforeSuccess)
            {
                throw new InvalidOperationException("Simulated transient failure.");
            }

            return Task.CompletedTask;
        }
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
