using System.Text.Json;
using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Infrastructure;
using AdminFlow.Budget.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace AdminFlow.Budget.IntegrationTests.Messaging;

[Collection(RabbitMqCollection.Name)]
public sealed class RabbitMqExpenseApprovedPublisherTests
{
    private const string Exchange = "adminflow.budget";
    private const string Queue = "adminflow.budget.expense-approved-test";
    private const string RoutingKey = "expense.approved";

    [RabbitMqFact]
    public async Task Publish_WhenRabbitMqIsAvailable_ShouldDeliverSerializedEvent()
    {
        var password = Environment.GetEnvironmentVariable(
            "ADMINFLOW_TEST_RABBITMQ_PASSWORD")!;
        var options = new RabbitMqOptions
        {
            Enabled = true,
            HostName = "localhost",
            Port = 5672,
            UserName = "adminflow",
            Password = password
        };
        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, durable: true);
        await channel.QueueDeclareAsync(Queue, durable: false, exclusive: false, autoDelete: true);
        await channel.QueueBindAsync(Queue, Exchange, RoutingKey);
        await channel.QueuePurgeAsync(Queue);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(
            "Host=localhost;Database=unused;Username=unused;Password=unused",
            options);
        await using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IExpenseApprovedPublisher>();
        var integrationEvent = new ExpenseApprovedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            250.50m,
            "BRL",
            DateTimeOffset.UtcNow);

        await publisher.PublishAsync(integrationEvent);

        var delivery = await channel.BasicGetAsync(Queue, autoAck: true);
        Assert.NotNull(delivery);
        var received = JsonSerializer.Deserialize<ExpenseApprovedIntegrationEvent>(
            delivery.Body.Span);
        Assert.Equal(integrationEvent, received);

        await channel.QueueDeleteAsync(Queue);
    }
}
