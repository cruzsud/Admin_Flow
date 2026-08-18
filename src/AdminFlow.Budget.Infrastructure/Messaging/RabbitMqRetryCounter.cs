using System.Collections;
using System.Text;
using RabbitMQ.Client;

namespace AdminFlow.Budget.Infrastructure.Messaging;

internal static class RabbitMqRetryCounter
{
    public static int GetAttemptCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null
            || !properties.Headers.TryGetValue("x-death", out var value)
            || value is not IEnumerable deaths)
        {
            return 0;
        }

        foreach (var death in deaths)
        {
            if (death is not IDictionary<string, object?> details
                || !details.TryGetValue("queue", out var queueValue)
                || GetText(queueValue) != RabbitMqTopology.Queue
                || !details.TryGetValue("count", out var countValue))
            {
                continue;
            }

            return countValue switch
            {
                long count => checked((int)count),
                int count => count,
                uint count => checked((int)count),
                ulong count => checked((int)count),
                _ => 0
            };
        }

        return 0;
    }

    private static string? GetText(object? value) => value switch
    {
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        ReadOnlyMemory<byte> bytes => Encoding.UTF8.GetString(bytes.Span),
        string text => text,
        _ => null
    };
}
