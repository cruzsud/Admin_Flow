namespace AdminFlow.Budget.Domain.ExpenseRequests;

public sealed class ExpenseRequest
{
    public ExpenseRequest(Guid budgetId, Guid requesterId, string description, decimal amount)
    {
        if (budgetId == Guid.Empty)
        {
            throw new ArgumentException("Budget id cannot be empty.", nameof(budgetId));
        }

        if (requesterId == Guid.Empty)
        {
            throw new ArgumentException("Requester id cannot be empty.", nameof(requesterId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException(
                "Amount cannot have more than two decimal places.",
                nameof(amount));
        }

        Id = Guid.NewGuid();
        BudgetId = budgetId;
        RequesterId = requesterId;
        Description = description.Trim();
        Amount = amount;
        Status = ExpenseRequestStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid BudgetId { get; private set; }
    public Guid RequesterId { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public ExpenseRequestStatus Status { get; private set; }
}
