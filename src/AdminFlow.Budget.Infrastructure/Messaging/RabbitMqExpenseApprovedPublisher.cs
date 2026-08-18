using System.Text.Json;
using AdminFlow.Budget.Application.IntegrationEvents;
using RabbitMQ.Client;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal sealed class RabbitMqExpenseApprovedPublisher(RabbitMqOptions options)
    : IExpenseApprovedPublisher
{
    public async Task PublishAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var factory = CreateConnectionFactory();
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.Exchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = integrationEvent.EventId.ToString()
        };

        await channel.BasicPublishAsync(
            RabbitMqTopology.Exchange,
            RabbitMqTopology.RoutingKey,
            mandatory: false,
            properties,
            body,
            cancellationToken);
    }

    private ConnectionFactory CreateConnectionFactory() => new()
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        VirtualHost = options.VirtualHost
    };
}
