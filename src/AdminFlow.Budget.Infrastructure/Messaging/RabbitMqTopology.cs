namespace AdminFlow.Budget.Infrastructure.Messaging;

internal static class RabbitMqTopology
{
    public const string Exchange = "adminflow.budget";
    public const string Queue = "adminflow.budget.expense-approved";
    public const string RoutingKey = "expense.approved";
    public const string RetryExchange = "adminflow.budget.retry";
    public const string RetryQueue = "adminflow.budget.expense-approved.retry";
    public const string RetryRoutingKey = "expense.approved.retry";
    public const string DeadLetterExchange = "adminflow.budget.dead-letter";
    public const string DeadLetterQueue = "adminflow.budget.expense-approved.dead-letter";
    public const string DeadLetterRoutingKey = "expense.approved.dead-letter";
}
