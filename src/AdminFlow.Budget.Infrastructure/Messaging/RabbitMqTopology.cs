namespace AdminFlow.Budget.Infrastructure.Messaging;

internal static class RabbitMqTopology
{
    public const string Exchange = "adminflow.budget";
    public const string Queue = "adminflow.budget.expense-approved";
    public const string RoutingKey = "expense.approved";
}
