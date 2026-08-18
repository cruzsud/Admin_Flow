using AdminFlow.Budget.Application.Approvals;
using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Domain.CostCenters;
using AdminFlow.Budget.Domain.ExpenseRequests;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class ExpenseApprovalPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset DecisionTime =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ADMINFLOW_TEST_DB_CONNECTION_STRING")!;

    [PostgreSqlFact]
    public async Task Approve_WhenBalanceIsEnough_ShouldPersistRequestAndBudgetTogether()
    {
        var (_, budget, request) = await SeedRequestAsync(1_000m, 400m);
        var decisionMakerId = Guid.NewGuid();

        await using (var context = CreateDbContext())
        {
            var service = new ExpenseApprovalService(
                context,
                new FixedTimeProvider(DecisionTime),
                NullLogger<ExpenseApprovalService>.Instance,
                NullExpenseApprovedPublisher.Instance);

            await service.ApproveAsync(request.Id, decisionMakerId);
        }

        await using var verificationContext = CreateDbContext();
        var persistedBudget = await verificationContext.Budgets.SingleAsync();
        var persistedRequest = await verificationContext.ExpenseRequests.SingleAsync();

        Assert.Equal(400m, persistedBudget.Committed);
        Assert.Equal(600m, persistedBudget.Available);
        Assert.Equal(ExpenseRequestStatus.Approved, persistedRequest.Status);
        Assert.Equal(decisionMakerId, persistedRequest.DecisionMakerId);
        Assert.Equal(DecisionTime, persistedRequest.DecidedAt);
        Assert.Null(persistedRequest.RejectionReason);
    }

    [PostgreSqlFact]
    public async Task Reject_WhenRequestIsPending_ShouldPersistDecisionWithoutCommittingBudget()
    {
        var (_, _, request) = await SeedRequestAsync(1_000m, 400m);

        await using (var context = CreateDbContext())
        {
            var service = new ExpenseApprovalService(
                context,
                new FixedTimeProvider(DecisionTime),
                NullLogger<ExpenseApprovalService>.Instance,
                NullExpenseApprovedPublisher.Instance);

            await service.RejectAsync(request.Id, Guid.NewGuid(), "Sem prioridade");
        }

        await using var verificationContext = CreateDbContext();
        var persistedBudget = await verificationContext.Budgets.SingleAsync();
        var persistedRequest = await verificationContext.ExpenseRequests.SingleAsync();

        Assert.Equal(0m, persistedBudget.Committed);
        Assert.Equal(ExpenseRequestStatus.Rejected, persistedRequest.Status);
        Assert.Equal("Sem prioridade", persistedRequest.RejectionReason);
    }

    [PostgreSqlFact]
    public async Task Approve_WhenBudgetWasChangedConcurrently_ShouldRollbackSecondApproval()
    {
        var costCenter = new CostCenter("ADM-001", "Administration");
        var budget = new BudgetEntity(costCenter.Id, 2026, 1_000m);
        var firstRequest = CreateRequest(budget.Id, 600m);
        var secondRequest = CreateRequest(budget.Id, 600m);

        await using (var seedContext = CreateDbContext())
        {
            seedContext.AddRange(costCenter, budget, firstRequest, secondRequest);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();

        var firstTrackedRequest = await firstContext.ExpenseRequests
            .SingleAsync(request => request.Id == firstRequest.Id);
        var firstTrackedBudget = await firstContext.Budgets.SingleAsync();
        var secondTrackedRequest = await secondContext.ExpenseRequests
            .SingleAsync(request => request.Id == secondRequest.Id);
        var secondTrackedBudget = await secondContext.Budgets.SingleAsync();

        firstTrackedRequest.Approve(Guid.NewGuid(), DecisionTime);
        firstTrackedBudget.Commit(firstTrackedRequest.Amount);
        secondTrackedRequest.Approve(Guid.NewGuid(), DecisionTime);
        secondTrackedBudget.Commit(secondTrackedRequest.Amount);

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());

        await using var verificationContext = CreateDbContext();
        var persistedBudget = await verificationContext.Budgets.SingleAsync();
        var persistedRequests = await verificationContext.ExpenseRequests
            .OrderBy(request => request.Id)
            .ToListAsync();

        Assert.Equal(600m, persistedBudget.Committed);
        Assert.Single(
            persistedRequests,
            request => request.Status == ExpenseRequestStatus.Approved);
        Assert.Single(
            persistedRequests,
            request => request.Status == ExpenseRequestStatus.Pending);
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

    private async Task<(CostCenter, BudgetEntity, ExpenseRequest)> SeedRequestAsync(
        decimal allocated,
        decimal requested)
    {
        var costCenter = new CostCenter("ADM-001", "Administration");
        var budget = new BudgetEntity(costCenter.Id, 2026, allocated);
        var request = CreateRequest(budget.Id, requested);

        await using var context = CreateDbContext();
        context.AddRange(costCenter, budget, request);
        await context.SaveChangesAsync();

        return (costCenter, budget, request);
    }

    private static ExpenseRequest CreateRequest(Guid budgetId, decimal amount)
    {
        return new ExpenseRequest(
            budgetId,
            Guid.NewGuid(),
            "Compra de materiais",
            amount);
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

    private sealed class FixedTimeProvider(DateTimeOffset currentTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => currentTime;
    }

    private sealed class NullExpenseApprovedPublisher : IExpenseApprovedPublisher
    {
        public static NullExpenseApprovedPublisher Instance { get; } = new();

        public Task PublishAsync(
            ExpenseApprovedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
