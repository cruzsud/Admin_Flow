using AdminFlow.Budget.Application.IntegrationEvents;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal interface IExpenseApprovedIntegrationEventProcessor
{
    Task<bool> ProcessAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
