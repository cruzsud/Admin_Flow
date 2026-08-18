using AdminFlow.Budget.Application.Approvals;
using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Infrastructure.Messaging;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminFlow.Budget.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        RabbitMqOptions rabbitMqOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<BudgetDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IExpenseApprovalStore>(services =>
            services.GetRequiredService<BudgetDbContext>());
        services.AddScoped<ExpenseApprovalService>();
        services.AddSingleton(TimeProvider.System);

        if (rabbitMqOptions.Enabled)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rabbitMqOptions.UserName);
            ArgumentException.ThrowIfNullOrWhiteSpace(rabbitMqOptions.Password);
            ArgumentOutOfRangeException.ThrowIfNegative(rabbitMqOptions.MaxRetryAttempts);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                rabbitMqOptions.RetryDelayMilliseconds);
            services.AddSingleton(rabbitMqOptions);
            services.AddSingleton<IExpenseApprovedPublisher, RabbitMqExpenseApprovedPublisher>();
            services.AddSingleton<IExpenseApprovedIntegrationEventHandler,
                LoggingExpenseApprovedIntegrationEventHandler>();
            services.AddHostedService<ExpenseApprovedConsumer>();
        }
        else
        {
            services.AddSingleton<IExpenseApprovedPublisher, DisabledExpenseApprovedPublisher>();
        }

        return services;
    }
}
