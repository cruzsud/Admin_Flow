using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace AdminFlow.Budget.Infrastructure.Observability;

public static class BudgetTelemetry
{
    public const string ActivitySourceName = "AdminFlow.Budget";
    public const string MeterName = "AdminFlow.Budget";

    private const string TraceParentHeader = "traceparent";
    private const string TraceStateHeader = "tracestate";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> RabbitMqMessages = Meter.CreateCounter<long>(
        "adminflow.budget.rabbitmq.messages",
        unit: "{message}",
        description: "Number of RabbitMQ messages by operation and outcome.");

    internal static Activity? StartRabbitMqPublishActivity(Guid eventId)
    {
        var activity = ActivitySource.StartActivity(
            "expense.approved publish",
            ActivityKind.Producer);
        AddRabbitMqTags(activity, "publish", eventId.ToString());
        return activity;
    }

    internal static Activity? StartRabbitMqConsumeActivity(
        IDictionary<string, object?>? headers,
        string? messageId)
    {
        var traceParent = GetHeaderText(headers, TraceParentHeader);
        var traceState = GetHeaderText(headers, TraceStateHeader);

        var activity = ActivityContext.TryParse(
            traceParent,
            traceState,
            isRemote: true,
            out var parentContext)
            ? ActivitySource.StartActivity(
                "expense.approved consume",
                ActivityKind.Consumer,
                parentContext)
            : ActivitySource.StartActivity(
                "expense.approved consume",
                ActivityKind.Consumer);

        AddRabbitMqTags(activity, "consume", messageId);
        return activity;
    }

    internal static void InjectTraceContext(IDictionary<string, object?> headers)
    {
        var activity = Activity.Current;
        if (activity?.Id is null)
        {
            return;
        }

        headers[TraceParentHeader] = Encoding.ASCII.GetBytes(activity.Id);

        if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
        {
            headers[TraceStateHeader] = Encoding.ASCII.GetBytes(
                activity.TraceStateString);
        }
    }

    internal static void RecordRabbitMqMessage(string operation, string outcome)
    {
        RabbitMqMessages.Add(
            1,
            new KeyValuePair<string, object?>("messaging.operation", operation),
            new KeyValuePair<string, object?>("messaging.outcome", outcome));
    }

    internal static void SetOutcome(Activity? activity, string outcome)
    {
        activity?.SetTag("messaging.outcome", outcome);
    }

    internal static void SetError(Activity? activity, Exception exception)
    {
        activity?.SetTag("error.type", exception.GetType().FullName);
        activity?.SetStatus(ActivityStatusCode.Error);
    }

    private static void AddRabbitMqTags(
        Activity? activity,
        string operation,
        string? messageId)
    {
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", "adminflow.budget");
        activity?.SetTag("messaging.operation", operation);

        if (!string.IsNullOrWhiteSpace(messageId))
        {
            activity?.SetTag("messaging.message.id", messageId);
        }
    }

    private static string? GetHeaderText(
        IDictionary<string, object?>? headers,
        string name)
    {
        if (headers is null || !headers.TryGetValue(name, out var value))
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.ASCII.GetString(bytes),
            ReadOnlyMemory<byte> bytes => Encoding.ASCII.GetString(bytes.Span),
            string text => text,
            _ => null
        };
    }
}
