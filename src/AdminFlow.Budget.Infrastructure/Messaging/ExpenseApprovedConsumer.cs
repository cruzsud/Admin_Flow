using System.Text.Json;
using AdminFlow.Budget.Application.IntegrationEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal sealed class ExpenseApprovedConsumer(
    RabbitMqOptions options,
    ILogger<ExpenseApprovedConsumer> logger,
    IExpenseApprovedIntegrationEventHandler handler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.Exchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.RetryExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            RabbitMqTopology.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RabbitMqTopology.RetryExchange,
                ["x-dead-letter-routing-key"] = RabbitMqTopology.RetryRoutingKey
            },
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            RabbitMqTopology.Queue,
            RabbitMqTopology.Exchange,
            RabbitMqTopology.RoutingKey,
            cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            RabbitMqTopology.RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = options.RetryDelayMilliseconds,
                ["x-dead-letter-exchange"] = RabbitMqTopology.Exchange,
                ["x-dead-letter-routing-key"] = RabbitMqTopology.RoutingKey
            },
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            RabbitMqTopology.RetryQueue,
            RabbitMqTopology.RetryExchange,
            RabbitMqTopology.RetryRoutingKey,
            cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            RabbitMqTopology.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            RabbitMqTopology.DeadLetterQueue,
            RabbitMqTopology.DeadLetterExchange,
            RabbitMqTopology.DeadLetterRoutingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var integrationEvent = JsonSerializer.Deserialize<ExpenseApprovedIntegrationEvent>(
                    eventArgs.Body.Span);

                if (integrationEvent is null
                    || !ExpenseApprovedIntegrationEventValidator.IsValid(integrationEvent))
                {
                    logger.LogWarning(
                        "RabbitMQ message {MessageId} contains an invalid expense approval event",
                        eventArgs.BasicProperties.MessageId);
                    await MoveToDeadLetterAsync(channel, eventArgs, stoppingToken);
                    return;
                }

                await handler.HandleAsync(integrationEvent, stoppingToken);

                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    stoppingToken);
            }
            catch (JsonException exception)
            {
                logger.LogWarning(
                    exception,
                    "RabbitMQ message {MessageId} is not valid JSON",
                    eventArgs.BasicProperties.MessageId);
                await MoveToDeadLetterAsync(channel, eventArgs, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var attemptCount = RabbitMqRetryCounter.GetAttemptCount(
                    eventArgs.BasicProperties);

                if (attemptCount < options.MaxRetryAttempts)
                {
                    logger.LogWarning(
                        exception,
                        "Failed to process RabbitMQ message {MessageId}; scheduling retry {RetryAttempt} of {MaxRetryAttempts}",
                        eventArgs.BasicProperties.MessageId,
                        attemptCount + 1,
                        options.MaxRetryAttempts);
                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        stoppingToken);
                }
                else
                {
                    logger.LogError(
                        exception,
                        "RabbitMQ message {MessageId} exhausted {MaxRetryAttempts} retries and will be dead-lettered",
                        eventArgs.BasicProperties.MessageId,
                        options.MaxRetryAttempts);
                    await MoveToDeadLetterAsync(channel, eventArgs, stoppingToken);
                }
            }
        };

        await channel.BasicConsumeAsync(
            RabbitMqTopology.Queue,
            autoAck: false,
            consumer,
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task MoveToDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            ContentType = eventArgs.BasicProperties.ContentType,
            DeliveryMode = eventArgs.BasicProperties.DeliveryMode,
            MessageId = eventArgs.BasicProperties.MessageId,
            Headers = eventArgs.BasicProperties.Headers
        };

        await channel.BasicPublishAsync(
            RabbitMqTopology.DeadLetterExchange,
            RabbitMqTopology.DeadLetterRoutingKey,
            mandatory: false,
            properties,
            eventArgs.Body,
            cancellationToken);
        await channel.BasicAckAsync(
            eventArgs.DeliveryTag,
            multiple: false,
            cancellationToken);
    }
}
