using AdminFlow.Budget.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal sealed class LoggingExpenseApprovedIntegrationEventHandler(
    ILogger<LoggingExpenseApprovedIntegrationEventHandler> logger)
    : IExpenseApprovedIntegrationEventHandler
{
    public Task HandleAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Integration event {EventId} for expense request {ExpenseRequestId} " +
            "and budget {BudgetId} was consumed",
            integrationEvent.EventId,
            integrationEvent.ExpenseRequestId,
            integrationEvent.BudgetId);

        return Task.CompletedTask;
    }
}
