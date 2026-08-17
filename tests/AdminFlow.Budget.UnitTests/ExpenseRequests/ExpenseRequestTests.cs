using AdminFlow.Budget.Domain.ExpenseRequests;

namespace AdminFlow.Budget.UnitTests.ExpenseRequests;

public sealed class ExpenseRequestTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreatePendingRequest()
    {
        var budgetId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        var request = new ExpenseRequest(
            budgetId,
            requesterId,
            "  Compra de materiais administrativos  ",
            1_250.50m);

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal(budgetId, request.BudgetId);
        Assert.Equal(requesterId, request.RequesterId);
        Assert.Equal("Compra de materiais administrativos", request.Description);
        Assert.Equal(1_250.50m, request.Amount);
        Assert.Equal(ExpenseRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Create_WithEmptyBudgetId_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ExpenseRequest(
                Guid.Empty,
                Guid.NewGuid(),
                "Compra de materiais",
                100m));

        Assert.Equal("budgetId", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyRequesterId_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ExpenseRequest(
                Guid.NewGuid(),
                Guid.Empty,
                "Compra de materiais",
                100m));

        Assert.Equal("requesterId", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingDescription_ShouldThrowArgumentException(
        string? invalidDescription)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ExpenseRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                invalidDescription!,
                100m));

        Assert.Equal("description", exception.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void Create_WithNonPositiveAmount_ShouldThrowArgumentOutOfRangeException(
        string invalidAmount)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExpenseRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Compra de materiais",
                decimal.Parse(invalidAmount)));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ExpenseRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Compra de materiais",
                100.001m));

        Assert.Equal("amount", exception.ParamName);
    }
}
