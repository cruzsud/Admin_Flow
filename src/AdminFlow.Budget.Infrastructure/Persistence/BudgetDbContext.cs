using AdminFlow.Budget.Application.Approvals;
using AdminFlow.Budget.Domain.CostCenters;
using AdminFlow.Budget.Domain.ExpenseRequests;
using Microsoft.EntityFrameworkCore;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.Infrastructure.Persistence;

public sealed class BudgetDbContext(DbContextOptions<BudgetDbContext> options)
    : DbContext(options), IExpenseApprovalStore
{
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();

    public DbSet<BudgetEntity> Budgets => Set<BudgetEntity>();

    public DbSet<ExpenseRequest> ExpenseRequests => Set<ExpenseRequest>();

    public Task<ExpenseRequest?> FindExpenseRequestAsync(
        Guid expenseRequestId,
        CancellationToken cancellationToken = default)
    {
        return ExpenseRequests.FirstOrDefaultAsync(
            request => request.Id == expenseRequestId,
            cancellationToken);
    }

    public Task<BudgetEntity?> FindBudgetAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default)
    {
        return Budgets.FirstOrDefaultAsync(
            budget => budget.Id == budgetId,
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetDbContext).Assembly);
    }
}
