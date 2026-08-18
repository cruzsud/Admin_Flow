using AdminFlow.Budget.Domain.ExpenseRequests;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.Application.Approvals;

public interface IExpenseApprovalStore
{
    Task<ExpenseRequest?> FindExpenseRequestAsync(
        Guid expenseRequestId,
        CancellationToken cancellationToken = default);

    Task<BudgetEntity?> FindBudgetAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
