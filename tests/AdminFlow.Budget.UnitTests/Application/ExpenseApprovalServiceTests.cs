using AdminFlow.Budget.Application.Approvals;
using AdminFlow.Budget.Domain.ExpenseRequests;
using Microsoft.Extensions.Logging;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.UnitTests.Application;

public sealed class ExpenseApprovalServiceTests
{
    private static readonly DateTimeOffset CurrentTime =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Approve_WhenRequestAndBudgetAreValid_ShouldCommitAndSaveOnce()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);
        var request = CreateRequest(budget.Id, 400m);
        var store = new FakeExpenseApprovalStore(request, budget);
        var service = CreateService(store);

        await service.ApproveAsync(request.Id, Guid.NewGuid());

        Assert.Equal(ExpenseRequestStatus.Approved, request.Status);
        Assert.Equal(400m, budget.Committed);
        Assert.Equal(600m, budget.Available);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task Approve_WhenSuccessful_ShouldWriteStructuredBusinessLog()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);
        var request = CreateRequest(budget.Id, 400m);
        var decisionMakerId = Guid.NewGuid();
        var store = new FakeExpenseApprovalStore(request, budget);
        var logger = new CollectingLogger<ExpenseApprovalService>();
        var service = CreateService(store, logger);

        await service.ApproveAsync(request.Id, decisionMakerId);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("ExpenseRequestApproved", entry.EventId.Name);
        Assert.Equal(request.Id, entry.Properties["ExpenseRequestId"]);
        Assert.Equal(budget.Id, entry.Properties["BudgetId"]);
        Assert.Equal(decisionMakerId, entry.Properties["DecisionMakerId"]);
        Assert.Equal(400m, entry.Properties["Amount"]);
        Assert.Equal("Approved", entry.Properties["Action"]);
        Assert.Equal(CurrentTime, entry.Properties["OccurredAt"]);
        Assert.DoesNotContain("Description", entry.Properties.Keys);
        Assert.DoesNotContain("RejectionReason", entry.Properties.Keys);
    }

    [Fact]
    public async Task Approve_WhenBalanceIsInsufficient_ShouldNotChangeOrSaveEntities()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 100m);
        var request = CreateRequest(budget.Id, 100.01m);
        var store = new FakeExpenseApprovalStore(request, budget);
        var service = CreateService(store);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(request.Id, Guid.NewGuid()));

        Assert.Equal(ExpenseRequestStatus.Pending, request.Status);
        Assert.Equal(0m, budget.Committed);
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task Approve_WhenBalanceIsInsufficient_ShouldNotWriteSuccessLog()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 100m);
        var request = CreateRequest(budget.Id, 100.01m);
        var store = new FakeExpenseApprovalStore(request, budget);
        var logger = new CollectingLogger<ExpenseApprovalService>();
        var service = CreateService(store, logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(request.Id, Guid.NewGuid()));

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Approve_WhenPersistenceFails_ShouldNotWriteSuccessLog()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);
        var request = CreateRequest(budget.Id, 400m);
        var store = new FakeExpenseApprovalStore(request, budget, shouldFailOnSave: true);
        var logger = new CollectingLogger<ExpenseApprovalService>();
        var service = CreateService(store, logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(request.Id, Guid.NewGuid()));

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Approve_WhenRequestDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var store = new FakeExpenseApprovalStore(null, null);
        var service = CreateService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ApproveAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task Reject_WhenRequestIsPending_ShouldRejectWithoutChangingBudget()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);
        var request = CreateRequest(budget.Id, 400m);
        var store = new FakeExpenseApprovalStore(request, budget);
        var service = CreateService(store);

        await service.RejectAsync(request.Id, Guid.NewGuid(), "Sem prioridade");

        Assert.Equal(ExpenseRequestStatus.Rejected, request.Status);
        Assert.Equal("Sem prioridade", request.RejectionReason);
        Assert.Equal(0m, budget.Committed);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task Reject_WhenSuccessful_ShouldWriteStructuredBusinessLogWithoutReason()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);
        var request = CreateRequest(budget.Id, 400m);
        var decisionMakerId = Guid.NewGuid();
        var store = new FakeExpenseApprovalStore(request, budget);
        var logger = new CollectingLogger<ExpenseApprovalService>();
        var service = CreateService(store, logger);

        await service.RejectAsync(request.Id, decisionMakerId, "Informação reservada");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("ExpenseRequestRejected", entry.EventId.Name);
        Assert.Equal(request.Id, entry.Properties["ExpenseRequestId"]);
        Assert.Equal(budget.Id, entry.Properties["BudgetId"]);
        Assert.Equal(decisionMakerId, entry.Properties["DecisionMakerId"]);
        Assert.Equal(400m, entry.Properties["Amount"]);
        Assert.Equal("Rejected", entry.Properties["Action"]);
        Assert.DoesNotContain("RejectionReason", entry.Properties.Keys);
        Assert.DoesNotContain("Informação reservada", entry.Message);
    }

    private static ExpenseApprovalService CreateService(
        FakeExpenseApprovalStore store,
        ILogger<ExpenseApprovalService>? logger = null)
    {
        return new ExpenseApprovalService(
            store,
            new FixedTimeProvider(CurrentTime),
            logger ?? new CollectingLogger<ExpenseApprovalService>());
    }

    private static ExpenseRequest CreateRequest(Guid budgetId, decimal amount)
    {
        return new ExpenseRequest(
            budgetId,
            Guid.NewGuid(),
            "Compra de materiais",
            amount);
    }

    private sealed class FakeExpenseApprovalStore(
        ExpenseRequest? request,
        BudgetEntity? budget,
        bool shouldFailOnSave = false) : IExpenseApprovalStore
    {
        public int SaveCalls { get; private set; }

        public Task<ExpenseRequest?> FindExpenseRequestAsync(
            Guid expenseRequestId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                request?.Id == expenseRequestId ? request : null);
        }

        public Task<BudgetEntity?> FindBudgetAsync(
            Guid budgetId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(budget?.Id == budgetId ? budget : null);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (shouldFailOnSave)
            {
                throw new InvalidOperationException("Persistence failed.");
            }

            SaveCalls++;
            return Task.FromResult(2);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset currentTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => currentTime;
    }

    private sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values
                    .Where(value => value.Key != "{OriginalFormat}")
                    .ToDictionary(value => value.Key, value => value.Value)
                : new Dictionary<string, object?>();

            Entries.Add(new LogEntry(
                logLevel,
                eventId,
                formatter(state, exception),
                properties));
        }
    }
}
