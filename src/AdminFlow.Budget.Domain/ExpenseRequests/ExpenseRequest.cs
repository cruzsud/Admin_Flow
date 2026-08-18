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
    public Guid? DecisionMakerId { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public void Approve(Guid decisionMakerId, DateTimeOffset decidedAt)
    {
        EnsurePending();
        EnsureDecisionDataIsValid(decisionMakerId, decidedAt);

        if (decisionMakerId == RequesterId)
        {
            throw new InvalidOperationException(
                "The requester cannot approve their own expense request.");
        }

        Status = ExpenseRequestStatus.Approved;
        DecisionMakerId = decisionMakerId;
        DecidedAt = decidedAt;
        RejectionReason = null;
    }

    public void Reject(Guid decisionMakerId, string reason, DateTimeOffset decidedAt)
    {
        EnsurePending();
        EnsureDecisionDataIsValid(decisionMakerId, decidedAt);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        }

        Status = ExpenseRequestStatus.Rejected;
        DecisionMakerId = decisionMakerId;
        DecidedAt = decidedAt;
        RejectionReason = reason.Trim();
    }

    private void EnsurePending()
    {
        if (Status != ExpenseRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending expense request can be decided.");
        }
    }

    private static void EnsureDecisionDataIsValid(
        Guid decisionMakerId,
        DateTimeOffset decidedAt)
    {
        if (decisionMakerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Decision maker id cannot be empty.",
                nameof(decisionMakerId));
        }

        if (decidedAt == default)
        {
            throw new ArgumentException(
                "Decision date cannot be empty.",
                nameof(decidedAt));
        }
    }
}
