using AdminFlow.Budget.Domain.CostCenters;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class BudgetPersistenceTests : IAsyncLifetime
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ADMINFLOW_TEST_DB_CONNECTION_STRING")!;

    [PostgreSqlFact]
    public async Task Save_WhenBudgetIsValid_ShouldPersistAndLoadIt()
    {
        var costCenter = new CostCenter("ADM-001", "Administration");
        var budget = new BudgetEntity(costCenter.Id, 2026, 150_000.75m);

        await using (var writeContext = CreateDbContext())
        {
            writeContext.AddRange(costCenter, budget);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext();
        var persisted = await readContext.Budgets.SingleAsync();

        Assert.Equal(budget.Id, persisted.Id);
        Assert.Equal(costCenter.Id, persisted.CostCenterId);
        Assert.Equal(2026, persisted.FiscalYear);
        Assert.Equal(150_000.75m, persisted.Allocated);
        Assert.Equal(0m, persisted.Committed);
        Assert.Equal(150_000.75m, persisted.Available);
    }

    [PostgreSqlFact]
    public async Task Save_WhenCostCenterAndFiscalYearAlreadyExist_ShouldRejectDuplicate()
    {
        var costCenter = new CostCenter("ADM-001", "Administration");

        await using var context = CreateDbContext();
        context.CostCenters.Add(costCenter);
        context.Budgets.AddRange(
            new BudgetEntity(costCenter.Id, 2026, 100_000m),
            new BudgetEntity(costCenter.Id, 2026, 200_000m));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Save_WhenFiscalYearIsSameForDifferentCostCenters_ShouldPersistBoth()
    {
        var firstCostCenter = new CostCenter("ADM-001", "Administration");
        var secondCostCenter = new CostCenter("TEC-001", "Technology");

        await using var context = CreateDbContext();
        context.CostCenters.AddRange(firstCostCenter, secondCostCenter);
        context.Budgets.AddRange(
            new BudgetEntity(firstCostCenter.Id, 2026, 100_000m),
            new BudgetEntity(secondCostCenter.Id, 2026, 200_000m));
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Budgets.CountAsync());
    }

    [PostgreSqlFact]
    public async Task Save_WhenCostCenterDoesNotExist_ShouldRejectBudget()
    {
        await using var context = CreateDbContext();
        context.Budgets.Add(new BudgetEntity(Guid.NewGuid(), 2026, 100_000m));

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
