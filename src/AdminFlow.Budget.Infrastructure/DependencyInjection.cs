using AdminFlow.Budget.Application.Approvals;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminFlow.Budget.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<BudgetDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IExpenseApprovalStore>(services =>
            services.GetRequiredService<BudgetDbContext>());
        services.AddScoped<ExpenseApprovalService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
