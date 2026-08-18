using AdminFlow.Budget.Application.IntegrationEvents;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal static class ExpenseApprovedIntegrationEventValidator
{
    public static bool IsValid(ExpenseApprovedIntegrationEvent integrationEvent)
    {
        return integrationEvent.EventId != Guid.Empty
            && integrationEvent.ExpenseRequestId != Guid.Empty
            && integrationEvent.BudgetId != Guid.Empty
            && integrationEvent.DecisionMakerId != Guid.Empty
            && integrationEvent.Amount > 0
            && integrationEvent.Currency == "BRL"
            && integrationEvent.ApprovedAt != default;
    }
}
