using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal sealed class IdempotentExpenseApprovedIntegrationEventProcessor(
    IDbContextFactory<BudgetDbContext> dbContextFactory,
    IExpenseApprovedIntegrationEventHandler handler,
    TimeProvider timeProvider,
    ILogger<IdempotentExpenseApprovedIntegrationEventProcessor> logger)
    : IExpenseApprovedIntegrationEventProcessor
{
    public async Task<bool> ProcessAsync(
        ExpenseApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken);

        context.ProcessedIntegrationEvents.Add(
            new ProcessedIntegrationEvent(
                integrationEvent.EventId,
                nameof(ExpenseApprovedIntegrationEvent),
                timeProvider.GetUtcNow()));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEvent(exception))
        {
            logger.LogInformation(
                "Integration event {EventId} was already processed and will be acknowledged",
                integrationEvent.EventId);
            return false;
        }

        await handler.HandleAsync(integrationEvent, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private static bool IsDuplicateEvent(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "pk_processed_integration_events"
        };
    }
}
