using AdminFlow.Budget.Application.IntegrationEvents;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal sealed class DisabledExpenseApprovedPublisher : IExpenseApprovedPublisher
{
    public Task PublishAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
