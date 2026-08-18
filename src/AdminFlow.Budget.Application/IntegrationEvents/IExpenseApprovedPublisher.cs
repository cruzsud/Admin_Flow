namespace AdminFlow.Budget.Application.IntegrationEvents;

public interface IExpenseApprovedPublisher
{
    Task PublishAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
