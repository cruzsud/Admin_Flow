using AdminFlow.Budget.Domain.CostCenters;
using Microsoft.EntityFrameworkCore;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.Infrastructure.Persistence;

public sealed class BudgetDbContext(DbContextOptions<BudgetDbContext> options)
    : DbContext(options)
{
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();

    public DbSet<BudgetEntity> Budgets => Set<BudgetEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetDbContext).Assembly);
    }
}
