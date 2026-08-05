namespace Notrelix.Platform.Messaging.Observability;

public abstract record DiagnosticEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
}

public sealed record EventPublishedEvent : DiagnosticEvent
{
    public string EventName { get; init; } = string.Empty;
    public int EventVersion { get; init; }
    public string EnvelopeId { get; init; } = string.Empty;
    public long DurationMs { get; init; }
}

public sealed record EventPublishFailedEvent : DiagnosticEvent
{
    public string EventName { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}

public sealed record DeliverySucceededEvent : DiagnosticEvent
{
    public string EventName { get; init; } = string.Empty;
    public string Consumer { get; init; } = string.Empty;
    public int RetryCount { get; init; }
}

public sealed record DeliveryFailedEvent : DiagnosticEvent
{
    public string EventName { get; init; } = string.Empty;
    public string Consumer { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public bool DeadLettered { get; init; }
}

public sealed record CircuitBreakerTrippedEvent : DiagnosticEvent
{
    public string CircuitName { get; init; } = string.Empty;
    public int FailureCount { get; init; }
}

public sealed record CircuitBreakerResetEvent : DiagnosticEvent
{
    public string CircuitName { get; init; } = string.Empty;
}
