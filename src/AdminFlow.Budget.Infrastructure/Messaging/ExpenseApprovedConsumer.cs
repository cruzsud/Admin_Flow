using System.Text.Json;
using AdminFlow.Budget.Application.IntegrationEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal sealed class ExpenseApprovedConsumer(
    RabbitMqOptions options,
    ILogger<ExpenseApprovedConsumer> logger) : BackgroundService
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
        await channel.QueueDeclareAsync(
            RabbitMqTopology.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            RabbitMqTopology.Queue,
            RabbitMqTopology.Exchange,
            RabbitMqTopology.RoutingKey,
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
                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        stoppingToken);
                    return;
                }

                logger.LogInformation(
                    "Integration event {EventId} for expense request {ExpenseRequestId} " +
                    "and budget {BudgetId} was consumed",
                    integrationEvent.EventId,
                    integrationEvent.ExpenseRequestId,
                    integrationEvent.BudgetId);

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
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(
                    exception,
                    "Failed to process RabbitMQ message {MessageId}; returning it to the queue",
                    eventArgs.BasicProperties.MessageId);
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            RabbitMqTopology.Queue,
            autoAck: false,
            consumer,
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
