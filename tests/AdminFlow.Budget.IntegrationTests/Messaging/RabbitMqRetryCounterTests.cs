using System.Text;
using AdminFlow.Budget.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace AdminFlow.Budget.IntegrationTests.Messaging;

public sealed class RabbitMqRetryCounterTests
{
    [Fact]
    public void GetAttemptCount_WhenMessageHasNoDeathHeader_ShouldReturnZero()
    {
        var properties = new BasicProperties();

        var count = RabbitMqRetryCounter.GetAttemptCount(properties);

        Assert.Equal(0, count);
    }

    [Fact]
    public void GetAttemptCount_WhenMainQueueHasDeathCount_ShouldReturnIt()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                ["x-death"] = new List<object>
                {
                    new Dictionary<string, object?>
                    {
                        ["queue"] = Encoding.UTF8.GetBytes(
                            "adminflow.budget.expense-approved"),
                        ["count"] = 3L
                    }
                }
            }
        };

        var count = RabbitMqRetryCounter.GetAttemptCount(properties);

        Assert.Equal(3, count);
    }

    [Fact]
    public void GetAttemptCount_WhenDeathBelongsToRetryQueue_ShouldIgnoreIt()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                ["x-death"] = new List<object>
                {
                    new Dictionary<string, object?>
                    {
                        ["queue"] = Encoding.UTF8.GetBytes(
                            "adminflow.budget.expense-approved.retry"),
                        ["count"] = 5L
                    }
                }
            }
        };

        var count = RabbitMqRetryCounter.GetAttemptCount(properties);

        Assert.Equal(0, count);
    }
}
