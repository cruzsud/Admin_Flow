using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdminFlow.Budget.Infrastructure.Persistence;

internal sealed class DesignTimeBudgetDbContextFactory
    : IDesignTimeDbContextFactory<BudgetDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "ConnectionStrings__BudgetDatabase";

    public BudgetDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} before using Entity Framework tools.");
        }

        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BudgetDbContext(options);
    }
}
