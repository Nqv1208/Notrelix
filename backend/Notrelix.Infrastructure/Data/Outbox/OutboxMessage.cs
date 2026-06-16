using System.Text.Json;
using Notrelix.Domain.Common;

namespace Notrelix.Infrastructure.Data.Outbox;

public sealed class OutboxMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public int EventVersion { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }
    public string PayloadJson { get; private set; } = null!;
    public string Status { get; private set; } = OutboxStatus.Pending;
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = OutboxDefaults.MaxRetries;
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage From(IDomainEvent domainEvent)
    {
        var now = DateTimeOffset.UtcNow;
        var eventType = domainEvent is IOutboxEvent outboxEvent
            ? outboxEvent.EventType
            : domainEvent.GetType().FullName!;

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            EventId = domainEvent.EventId,
            EventType = eventType,
            EventVersion = domainEvent.EventVersion,
            WorkspaceId = domainEvent.WorkspaceId,
            ActorUserId = domainEvent.ActorUserId,
            CorrelationId = domainEvent.CorrelationId,
            CausationId = domainEvent.CausationId,
            PayloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
            Status = OutboxStatus.Pending,
            NextAttemptAt = now,
            CreatedAt = now,
        };
    }

    public void MarkProcessing()
    {
        Status = OutboxStatus.Processing;
    }

    public void MarkProcessed()
    {
        Status = OutboxStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;

        if (RetryCount >= MaxRetries)
        {
            Status = OutboxStatus.DeadLetter;
        }
        else
        {
            Status = OutboxStatus.Failed;
            NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(
                Math.Min(Math.Pow(2, RetryCount), OutboxDefaults.MaxBackoffSeconds));
        }
    }
}

public static class OutboxDefaults
{
    public const int MaxRetries = 5;
    public const int MaxBackoffSeconds = 60;
    public const int BatchSize = 20;
    public const int PollIntervalMs = 5000;
}

public static class OutboxStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
    public const string DeadLetter = "DeadLetter";
}
