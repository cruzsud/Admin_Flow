using AdminFlow.Budget.Domain.CostCenters;
using AdminFlow.Budget.Domain.ExpenseRequests;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class ExpenseRequestPersistenceTests : IAsyncLifetime
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ADMINFLOW_TEST_DB_CONNECTION_STRING")!;

    [PostgreSqlFact]
    public async Task Save_WhenRequestIsValid_ShouldPersistAndLoadIt()
    {
        var costCenter = new CostCenter("ADM-001", "Administration");
        var budget = new BudgetEntity(costCenter.Id, 2026, 100_000m);
        var requesterId = Guid.NewGuid();
        var request = new ExpenseRequest(
            budget.Id,
            requesterId,
            "Compra de materiais administrativos",
            1_250.50m);

        await using (var writeContext = CreateDbContext())
        {
            writeContext.AddRange(costCenter, budget, request);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext();
        var persisted = await readContext.ExpenseRequests.SingleAsync(
            savedRequest => savedRequest.BudgetId == budget.Id);

        Assert.Equal(request.Id, persisted.Id);
        Assert.Equal(budget.Id, persisted.BudgetId);
        Assert.Equal(requesterId, persisted.RequesterId);
        Assert.Equal("Compra de materiais administrativos", persisted.Description);
        Assert.Equal(1_250.50m, persisted.Amount);
        Assert.Equal(ExpenseRequestStatus.Pending, persisted.Status);
    }

    [PostgreSqlFact]
    public async Task Save_WhenBudgetDoesNotExist_ShouldRejectRequest()
    {
        await using var context = CreateDbContext();
        context.ExpenseRequests.Add(new ExpenseRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Compra de materiais",
            100m));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Query_WhenBudgetHasRequests_ShouldReturnOnlyItsRequests()
    {
        var firstCostCenter = new CostCenter("ADM-001", "Administration");
        var secondCostCenter = new CostCenter("TEC-001", "Technology");
        var firstBudget = new BudgetEntity(firstCostCenter.Id, 2026, 100_000m);
        var secondBudget = new BudgetEntity(secondCostCenter.Id, 2026, 200_000m);

        await using var context = CreateDbContext();
        context.AddRange(firstCostCenter, secondCostCenter, firstBudget, secondBudget);
        context.ExpenseRequests.AddRange(
            new ExpenseRequest(firstBudget.Id, Guid.NewGuid(), "Material", 100m),
            new ExpenseRequest(firstBudget.Id, Guid.NewGuid(), "Serviço", 200m),
            new ExpenseRequest(secondBudget.Id, Guid.NewGuid(), "Equipamento", 300m));
        await context.SaveChangesAsync();

        var requests = await context.ExpenseRequests
            .Where(request => request.BudgetId == firstBudget.Id)
            .ToListAsync();

        Assert.Equal(2, requests.Count);
        Assert.All(requests, request => Assert.Equal(firstBudget.Id, request.BudgetId));
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
        await context.ExpenseRequests.ExecuteDeleteAsync();
        await context.Budgets.ExecuteDeleteAsync();
        await context.CostCenters.ExecuteDeleteAsync();
    }
}
