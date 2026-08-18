using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Infrastructure.Messaging;

namespace AdminFlow.Budget.IntegrationTests.Messaging;

public sealed class ExpenseApprovedIntegrationEventValidatorTests
{
    [Fact]
    public void IsValid_WhenEventIsComplete_ShouldReturnTrue()
    {
        var integrationEvent = CreateValidEvent();

        var isValid = ExpenseApprovedIntegrationEventValidator.IsValid(integrationEvent);

        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_WhenIdentifierIsEmpty_ShouldReturnFalse()
    {
        var integrationEvent = CreateValidEvent() with { EventId = Guid.Empty };

        var isValid = ExpenseApprovedIntegrationEventValidator.IsValid(integrationEvent);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void IsValid_WhenAmountIsNotPositive_ShouldReturnFalse(string amount)
    {
        var integrationEvent = CreateValidEvent() with { Amount = decimal.Parse(amount) };

        var isValid = ExpenseApprovedIntegrationEventValidator.IsValid(integrationEvent);

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenCurrencyIsNotBrl_ShouldReturnFalse()
    {
        var integrationEvent = CreateValidEvent() with { Currency = "USD" };

        var isValid = ExpenseApprovedIntegrationEventValidator.IsValid(integrationEvent);

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenApprovedAtIsEmpty_ShouldReturnFalse()
    {
        var integrationEvent = CreateValidEvent() with { ApprovedAt = default };

        var isValid = ExpenseApprovedIntegrationEventValidator.IsValid(integrationEvent);

        Assert.False(isValid);
    }

    private static ExpenseApprovedIntegrationEvent CreateValidEvent() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        100m,
        "BRL",
        DateTimeOffset.UtcNow);
}
