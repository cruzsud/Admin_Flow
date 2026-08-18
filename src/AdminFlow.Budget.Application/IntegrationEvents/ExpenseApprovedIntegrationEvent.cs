namespace AdminFlow.Budget.Application.IntegrationEvents;

public sealed record ExpenseApprovedIntegrationEvent(
    Guid EventId,
    Guid ExpenseRequestId,
    Guid BudgetId,
    Guid DecisionMakerId,
    decimal Amount,
    string Currency,
    DateTimeOffset ApprovedAt);
