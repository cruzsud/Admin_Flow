using AdminFlow.Budget.Application.IntegrationEvents;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal interface IExpenseApprovedIntegrationEventHandler
{
    Task HandleAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken);
}
