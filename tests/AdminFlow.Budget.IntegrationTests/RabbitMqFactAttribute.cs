namespace AdminFlow.Budget.IntegrationTests;

public sealed class RabbitMqFactAttribute : FactAttribute
{
    public RabbitMqFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ADMINFLOW_TEST_RABBITMQ_PASSWORD")))
        {
            Skip = "Set ADMINFLOW_TEST_RABBITMQ_PASSWORD to run RabbitMQ integration tests.";
        }
    }
}
