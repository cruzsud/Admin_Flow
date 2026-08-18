namespace AdminFlow.Budget.IntegrationTests.Messaging;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RabbitMqCollection
{
    public const string Name = "RabbitMQ messaging";
}
