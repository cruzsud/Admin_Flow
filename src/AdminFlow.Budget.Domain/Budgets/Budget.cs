namespace AdminFlow.Budget.Domain.Budgets;

public sealed class Budget
{
    public Budget(Guid costCenterId, int fiscalYear, decimal allocated)
    {
        if (costCenterId == Guid.Empty)
        {
            throw new ArgumentException("Cost center id cannot be empty.", nameof(costCenterId));
        }

        if (fiscalYear is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fiscalYear),
                "Fiscal year must be between 1 and 9999.");
        }

        if (allocated <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(allocated),
                "Allocated amount must be greater than zero.");
        }

        if (decimal.Round(allocated, 2) != allocated)
        {
            throw new ArgumentException(
                "Allocated amount cannot have more than two decimal places.",
                nameof(allocated));
        }

        Id = Guid.NewGuid();
        CostCenterId = costCenterId;
        FiscalYear = fiscalYear;
        Allocated = allocated;
        Committed = 0m;
    }

    public Guid Id { get; private set; }

    public Guid CostCenterId { get; private set; }

    public int FiscalYear { get; private set; }

    public decimal Allocated { get; private set; }

    public decimal Committed { get; private set; }

    public decimal Available => Allocated - Committed;

    public void Commit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Committed amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException(
                "Committed amount cannot have more than two decimal places.",
                nameof(amount));
        }

        if (amount > Available)
        {
            throw new InvalidOperationException(
                "The budget does not have enough available balance.");
        }

        Committed += amount;
    }
}
