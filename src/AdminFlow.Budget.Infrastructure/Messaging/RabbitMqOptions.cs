namespace AdminFlow.Budget.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public bool Enabled { get; init; }
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = "/";
    public int MaxRetryAttempts { get; init; } = 3;
    public int RetryDelayMilliseconds { get; init; } = 5_000;
}
