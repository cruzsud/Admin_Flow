using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.UnitTests.Budgets;

public sealed class BudgetTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateAvailableBudget()
    {
        var costCenterId = Guid.NewGuid();

        var budget = new BudgetEntity(costCenterId, 2026, 150_000.75m);

        Assert.NotEqual(Guid.Empty, budget.Id);
        Assert.Equal(costCenterId, budget.CostCenterId);
        Assert.Equal(2026, budget.FiscalYear);
        Assert.Equal(150_000.75m, budget.Allocated);
        Assert.Equal(0m, budget.Committed);
        Assert.Equal(150_000.75m, budget.Available);
    }

    [Fact]
    public void Create_WithEmptyCostCenterId_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new BudgetEntity(Guid.Empty, 2026, 150_000m));

        Assert.Equal("costCenterId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_000)]
    public void Create_WithInvalidFiscalYear_ShouldThrowArgumentOutOfRangeException(
        int invalidFiscalYear)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BudgetEntity(Guid.NewGuid(), invalidFiscalYear, 150_000m));

        Assert.Equal("fiscalYear", exception.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void Create_WithNonPositiveAllocation_ShouldThrowArgumentOutOfRangeException(
        string invalidAllocation)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BudgetEntity(Guid.NewGuid(), 2026, decimal.Parse(invalidAllocation)));

        Assert.Equal("allocated", exception.ParamName);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new BudgetEntity(Guid.NewGuid(), 2026, 100.001m));

        Assert.Equal("allocated", exception.ParamName);
    }

    [Fact]
    public void Commit_WhenBalanceIsEnough_ShouldIncreaseCommittedAndDecreaseAvailable()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);

        budget.Commit(400m);

        Assert.Equal(400m, budget.Committed);
        Assert.Equal(600m, budget.Available);
    }

    [Fact]
    public void Commit_WhenAmountEqualsAvailable_ShouldUseEntireBalance()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);

        budget.Commit(1_000m);

        Assert.Equal(1_000m, budget.Committed);
        Assert.Equal(0m, budget.Available);
    }

    [Fact]
    public void Commit_WhenBalanceIsInsufficient_ShouldThrowInvalidOperationException()
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);

        var exception = Assert.Throws<InvalidOperationException>(() => budget.Commit(1_000.01m));

        Assert.Equal("The budget does not have enough available balance.", exception.Message);
        Assert.Equal(0m, budget.Committed);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void Commit_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(
        string invalidAmount)
    {
        var budget = new BudgetEntity(Guid.NewGuid(), 2026, 1_000m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => budget.Commit(decimal.Parse(invalidAmount)));
    }
}
