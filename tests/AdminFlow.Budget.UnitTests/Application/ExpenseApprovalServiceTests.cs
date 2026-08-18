using AdminFlow.Budget.Application.Approvals;
using AdminFlow.Budget.Domain.ExpenseRequests;
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

    private static ExpenseApprovalService CreateService(FakeExpenseApprovalStore store)
    {
        return new ExpenseApprovalService(store, new FixedTimeProvider(CurrentTime));
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
        BudgetEntity? budget) : IExpenseApprovalStore
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
            SaveCalls++;
            return Task.FromResult(2);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset currentTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => currentTime;
    }
}
