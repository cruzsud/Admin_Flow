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

    [Fact]
    public void Approve_WhenPendingAndDecisionMakerIsValid_ShouldApproveRequest()
    {
        var request = CreateRequest();
        var decisionMakerId = Guid.NewGuid();
        var decidedAt = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

        request.Approve(decisionMakerId, decidedAt);

        Assert.Equal(ExpenseRequestStatus.Approved, request.Status);
        Assert.Equal(decisionMakerId, request.DecisionMakerId);
        Assert.Equal(decidedAt, request.DecidedAt);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void Approve_WhenDecisionMakerIsRequester_ShouldThrowInvalidOperationException()
    {
        var requesterId = Guid.NewGuid();
        var request = CreateRequest(requesterId);

        Assert.Throws<InvalidOperationException>(
            () => request.Approve(requesterId, DateTimeOffset.UtcNow));

        Assert.Equal(ExpenseRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldThrowInvalidOperationException()
    {
        var request = CreateRequest();
        request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Approve_WhenDecisionMakerIdIsEmpty_ShouldThrowArgumentException()
    {
        var request = CreateRequest();

        Assert.Throws<ArgumentException>(
            () => request.Approve(Guid.Empty, DateTimeOffset.UtcNow));

        Assert.Equal(ExpenseRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Approve_WhenDecisionDateIsEmpty_ShouldThrowArgumentException()
    {
        var request = CreateRequest();

        Assert.Throws<ArgumentException>(
            () => request.Approve(Guid.NewGuid(), default));

        Assert.Equal(ExpenseRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Reject_WhenPendingAndReasonIsValid_ShouldRejectRequest()
    {
        var request = CreateRequest();
        var decisionMakerId = Guid.NewGuid();
        var decidedAt = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

        request.Reject(decisionMakerId, "  Fora das prioridades atuais  ", decidedAt);

        Assert.Equal(ExpenseRequestStatus.Rejected, request.Status);
        Assert.Equal(decisionMakerId, request.DecisionMakerId);
        Assert.Equal(decidedAt, request.DecidedAt);
        Assert.Equal("Fora das prioridades atuais", request.RejectionReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WhenReasonIsMissing_ShouldThrowArgumentException(string? reason)
    {
        var request = CreateRequest();

        Assert.Throws<ArgumentException>(
            () => request.Reject(Guid.NewGuid(), reason!, DateTimeOffset.UtcNow));

        Assert.Equal(ExpenseRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ShouldThrowInvalidOperationException()
    {
        var request = CreateRequest();
        request.Reject(Guid.NewGuid(), "Sem prioridade", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => request.Reject(Guid.NewGuid(), "Outro motivo", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reject_WhenRequestIsApproved_ShouldThrowInvalidOperationException()
    {
        var request = CreateRequest();
        request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => request.Reject(Guid.NewGuid(), "Outro motivo", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Approve_WhenRequestIsRejected_ShouldThrowInvalidOperationException()
    {
        var request = CreateRequest();
        request.Reject(Guid.NewGuid(), "Sem prioridade", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    private static ExpenseRequest CreateRequest(Guid? requesterId = null)
    {
        return new ExpenseRequest(
            Guid.NewGuid(),
            requesterId ?? Guid.NewGuid(),
            "Compra de materiais",
            100m);
    }
}
