using AdminFlow.Budget.Domain.ExpenseRequests;
using Microsoft.Extensions.Logging;

namespace AdminFlow.Budget.Application.Approvals;

public sealed class ExpenseApprovalService(
    IExpenseApprovalStore store,
    TimeProvider timeProvider,
    ILogger<ExpenseApprovalService> logger)
{
    private static readonly EventId ExpenseRequestApproved =
        new(1001, nameof(ExpenseRequestApproved));

    private static readonly EventId ExpenseRequestRejected =
        new(1002, nameof(ExpenseRequestRejected));

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

        var occurredAt = timeProvider.GetUtcNow();
        request.Approve(decisionMakerId, occurredAt);
        budget.Commit(request.Amount);

        await store.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            ExpenseRequestApproved,
            "Expense request {ExpenseRequestId} for budget {BudgetId} was {Action} " +
            "by {DecisionMakerId} for {Amount} at {OccurredAt}",
            request.Id,
            budget.Id,
            "Approved",
            decisionMakerId,
            request.Amount,
            occurredAt);
    }

    public async Task RejectAsync(
        Guid expenseRequestId,
        Guid decisionMakerId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var request = await FindRequestAsync(expenseRequestId, cancellationToken);

        var occurredAt = timeProvider.GetUtcNow();
        request.Reject(decisionMakerId, reason, occurredAt);

        await store.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            ExpenseRequestRejected,
            "Expense request {ExpenseRequestId} for budget {BudgetId} was {Action} " +
            "by {DecisionMakerId} for {Amount} at {OccurredAt}",
            request.Id,
            request.BudgetId,
            "Rejected",
            decisionMakerId,
            request.Amount,
            occurredAt);
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
