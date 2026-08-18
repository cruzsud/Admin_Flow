namespace AdminFlow.Budget.Infrastructure.Persistence;

internal sealed class ProcessedIntegrationEvent(
    Guid eventId,
    string eventType,
    DateTimeOffset processedAt)
{
    public Guid EventId { get; private set; } = eventId;

    public string EventType { get; private set; } = eventType;

    public DateTimeOffset ProcessedAt { get; private set; } = processedAt;
}
