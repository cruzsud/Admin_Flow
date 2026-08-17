using AdminFlow.Budget.Domain.CostCenters;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminFlow.Budget.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class CostCenterPersistenceTests : IAsyncLifetime
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ADMINFLOW_TEST_DB_CONNECTION_STRING")!;

    [PostgreSqlFact]
    public async Task Save_WhenCostCenterIsValid_ShouldPersistAndLoadIt()
    {
        var costCenter = new CostCenter("ADM-001", "Administration");

        await using (var writeContext = CreateDbContext())
        {
            writeContext.CostCenters.Add(costCenter);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext();
        var persisted = await readContext.CostCenters.SingleAsync();

        Assert.Equal(costCenter.Id, persisted.Id);
        Assert.Equal("ADM-001", persisted.Code);
        Assert.Equal("Administration", persisted.Name);
    }

    [PostgreSqlFact]
    public async Task Save_WhenCodeAlreadyExists_ShouldRejectDuplicate()
    {
        await using var context = CreateDbContext();
        context.CostCenters.AddRange(
            new CostCenter("ADM-001", "Administration"),
            new CostCenter("ADM-001", "Another Administration"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
        await ResetDatabaseAsync(context);
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateDbContext();
        await ResetDatabaseAsync(context);
    }

    private BudgetDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new BudgetDbContext(options);
    }

    private static async Task ResetDatabaseAsync(BudgetDbContext context)
    {
        await context.Budgets.ExecuteDeleteAsync();
        await context.CostCenters.ExecuteDeleteAsync();
    }
}
