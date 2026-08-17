namespace AdminFlow.Budget.IntegrationTests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ADMINFLOW_TEST_DB_CONNECTION_STRING")))
        {
            Skip = "Set ADMINFLOW_TEST_DB_CONNECTION_STRING to run PostgreSQL integration tests.";
        }
    }
}
