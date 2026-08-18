using AdminFlow.Budget.Domain.ExpenseRequests;

namespace AdminFlow.Budget.Application.Approvals;

public sealed class ExpenseApprovalService(
    IExpenseApprovalStore store,
    TimeProvider timeProvider)
{
    public async Task ApproveAsync(
        Guid expenseRequestId,
        Guid decisionMakerId,
        CancellationToken cancellationToken = default)
    {
        var request = await FindRequestAsync(expenseRequestId, cancellationToken);
        var budget = await store.FindBudgetAsync(request.BudgetId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Budget '{request.BudgetId}' was not found.");

        if (request.Amount > budget.Available)
        {
            throw new InvalidOperationException(
                "The budget does not have enough available balance.");
        }

        request.Approve(decisionMakerId, timeProvider.GetUtcNow());
        budget.Commit(request.Amount);

        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(
        Guid expenseRequestId,
        Guid decisionMakerId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var request = await FindRequestAsync(expenseRequestId, cancellationToken);

        request.Reject(decisionMakerId, reason, timeProvider.GetUtcNow());

        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task<ExpenseRequest> FindRequestAsync(
        Guid expenseRequestId,
        CancellationToken cancellationToken)
    {
        return await store.FindExpenseRequestAsync(expenseRequestId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Expense request '{expenseRequestId}' was not found.");
    }
}
