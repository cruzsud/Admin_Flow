using AdminFlow.Budget.Application.Approvals;
using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Infrastructure.Messaging;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AdminFlow.Budget.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        RabbitMqOptions rabbitMqOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton(_ =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString)
            {
                Name = "AdminFlow.Budget.Database"
            };
            return dataSourceBuilder.Build();
        });
        services.AddDbContextFactory<BudgetDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                serviceProvider.GetRequiredService<NpgsqlDataSource>()));
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
            services.AddSingleton<IExpenseApprovedIntegrationEventProcessor,
                IdempotentExpenseApprovedIntegrationEventProcessor>();
            services.AddHostedService<ExpenseApprovedConsumer>();
        }
        else
        {
            services.AddSingleton<IExpenseApprovedPublisher, DisabledExpenseApprovedPublisher>();
        }

        return services;
    }
}
