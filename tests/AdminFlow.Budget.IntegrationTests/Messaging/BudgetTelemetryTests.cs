using System.Diagnostics;
using System.Diagnostics.Metrics;
using AdminFlow.Budget.Infrastructure.Observability;

namespace AdminFlow.Budget.IntegrationTests.Messaging;

[Collection(RabbitMqCollection.Name)]
public sealed class BudgetTelemetryTests
{
    [Fact]
    public void RabbitMqActivities_WhenContextIsPropagated_ShouldShareTrace()
    {
        using var listener = CreateActivityListener();
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("incoming-request")
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();
        var headers = new Dictionary<string, object?>();
        var eventId = Guid.NewGuid();

        using var producer = BudgetTelemetry.StartRabbitMqPublishActivity(eventId);
        Assert.NotNull(producer);
        BudgetTelemetry.InjectTraceContext(headers);
        producer.Stop();
        using var consumer = BudgetTelemetry.StartRabbitMqConsumeActivity(
            headers,
            eventId.ToString());

        Assert.NotNull(consumer);
        Assert.Equal(producer.TraceId, consumer.TraceId);
        Assert.Equal(producer.SpanId, consumer.ParentSpanId);
    }

    [Fact]
    public void RecordRabbitMqMessage_ShouldEmitLowCardinalityOutcomeMetric()
    {
        long measurement = 0;
        string? operation = null;
        string? outcome = null;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == BudgetTelemetry.MeterName
                    && instrument.Name == "adminflow.budget.rabbitmq.messages")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            measurement += value;
            foreach (var tag in tags)
            {
                if (tag.Key == "messaging.operation")
                {
                    operation = tag.Value?.ToString();
                }
                else if (tag.Key == "messaging.outcome")
                {
                    outcome = tag.Value?.ToString();
                }
            }
        });
        listener.Start();

        BudgetTelemetry.RecordRabbitMqMessage("consume", "processed");

        Assert.Equal(1, measurement);
        Assert.Equal("consume", operation);
        Assert.Equal("processed", outcome);
    }

    private static ActivityListener CreateActivityListener() => new()
    {
        ShouldListenTo = source => source.Name == BudgetTelemetry.ActivitySourceName,
        Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllDataAndRecorded
    };
}
