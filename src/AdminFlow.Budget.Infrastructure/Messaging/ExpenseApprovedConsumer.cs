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
        consumer.ReceivedAsync += (_, eventArgs) =>
        {
            var integrationEvent = JsonSerializer.Deserialize<ExpenseApprovedIntegrationEvent>(
                eventArgs.Body.Span);

            if (integrationEvent is not null)
            {
                logger.LogInformation(
                    "Integration event {EventId} for expense request {ExpenseRequestId} " +
                    "and budget {BudgetId} was consumed",
                    integrationEvent.EventId,
                    integrationEvent.ExpenseRequestId,
                    integrationEvent.BudgetId);
            }

            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(
            RabbitMqTopology.Queue,
            autoAck: true,
            consumer,
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
