namespace AdminFlow.Budget.IntegrationTests.Persistence;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlCollection
{
    public const string Name = "PostgreSQL persistence";
}
