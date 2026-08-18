using AdminFlow.Budget.Application.IntegrationEvents;
using AdminFlow.Budget.Infrastructure.Messaging;
using AdminFlow.Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AdminFlow.Budget.IntegrationTests.Messaging;

[Collection(Persistence.PostgreSqlCollection.Name)]
public sealed class IdempotentExpenseApprovedIntegrationEventProcessorTests : IAsyncLifetime
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ADMINFLOW_TEST_DB_CONNECTION_STRING")!;

    [PostgreSqlFact]
    public async Task Process_WhenEventWasAlreadyProcessed_ShouldNotCallHandlerAgain()
    {
        var handler = new RecordingHandler();
        var processor = CreateProcessor(handler);
        var integrationEvent = CreateEvent();

        var firstResult = await processor.ProcessAsync(integrationEvent);
        var duplicateResult = await processor.ProcessAsync(integrationEvent);

        Assert.True(firstResult);
        Assert.False(duplicateResult);
        Assert.Equal(1, handler.CallCount);
    }

    [PostgreSqlFact]
    public async Task Process_WhenHandlerFails_ShouldRollbackMarkerAndAllowRetry()
    {
        var handler = new RecordingHandler(failuresBeforeSuccess: 1);
        var processor = CreateProcessor(handler);
        var integrationEvent = CreateEvent();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => processor.ProcessAsync(integrationEvent));
        var retryResult = await processor.ProcessAsync(integrationEvent);

        Assert.True(retryResult);
        Assert.Equal(2, handler.CallCount);
    }

    [PostgreSqlFact]
    public async Task Process_WhenDuplicateArrivesConcurrently_ShouldCallHandlerOnce()
    {
        var handler = new BlockingHandler();
        var processor = CreateProcessor(handler);
        var integrationEvent = CreateEvent();

        var firstProcessing = processor.ProcessAsync(integrationEvent);
        await handler.WaitUntilStartedAsync();
        var duplicateProcessing = processor.ProcessAsync(integrationEvent);
        handler.Release();
        var results = await Task.WhenAll(firstProcessing, duplicateProcessing);

        Assert.Contains(true, results);
        Assert.Contains(false, results);
        Assert.Equal(1, handler.CallCount);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE processed_integration_events");
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE processed_integration_events");
    }

    private IdempotentExpenseApprovedIntegrationEventProcessor CreateProcessor(
        IExpenseApprovedIntegrationEventHandler handler)
    {
        return new IdempotentExpenseApprovedIntegrationEventProcessor(
            new TestDbContextFactory(_connectionString),
            handler,
            TimeProvider.System,
            NullLogger<IdempotentExpenseApprovedIntegrationEventProcessor>.Instance);
    }

    private BudgetDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new BudgetDbContext(options);
    }

    private static ExpenseApprovedIntegrationEvent CreateEvent()
    {
        return new ExpenseApprovedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            "BRL",
            DateTimeOffset.UtcNow);
    }

    private sealed class TestDbContextFactory(string connectionString)
        : IDbContextFactory<BudgetDbContext>
    {
        public BudgetDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BudgetDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new BudgetDbContext(options);
        }
    }

    private sealed class RecordingHandler(int failuresBeforeSuccess = 0)
        : IExpenseApprovedIntegrationEventHandler
    {
        private int _callCount;

        public int CallCount => _callCount;

        public Task HandleAsync(
            ExpenseApprovedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken)
        {
            var currentCall = Interlocked.Increment(ref _callCount);
            if (currentCall <= failuresBeforeSuccess)
            {
                throw new InvalidOperationException("Simulated processing failure.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class BlockingHandler : IExpenseApprovedIntegrationEventHandler
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public int CallCount => _callCount;

        public async Task HandleAsync(
            ExpenseApprovedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            _started.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }

        public Task WaitUntilStartedAsync() => _started.Task;

        public void Release() => _release.TrySetResult();
    }
}
