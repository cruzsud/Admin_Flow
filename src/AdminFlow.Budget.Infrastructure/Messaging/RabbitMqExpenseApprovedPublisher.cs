using System.Text.Json;
using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Infrastructure.Observability;
using RabbitMQ.Client;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal sealed class RabbitMqExpenseApprovedPublisher(RabbitMqOptions options)
    : IExpenseApprovedPublisher
{
    public async Task PublishAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        using var activity = BudgetTelemetry.StartRabbitMqPublishActivity(
            integrationEvent.EventId);

        try
        {
            await PublishCoreAsync(integrationEvent, cancellationToken);
            BudgetTelemetry.SetOutcome(activity, "published");
            BudgetTelemetry.RecordRabbitMqMessage("publish", "published");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            BudgetTelemetry.SetError(activity, exception);
            BudgetTelemetry.SetOutcome(activity, "failed");
            BudgetTelemetry.RecordRabbitMqMessage("publish", "failed");
            throw;
        }
    }

    private async Task PublishCoreAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
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
            MessageId = integrationEvent.EventId.ToString(),
            Headers = new Dictionary<string, object?>()
        };
        BudgetTelemetry.InjectTraceContext(properties.Headers);

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
