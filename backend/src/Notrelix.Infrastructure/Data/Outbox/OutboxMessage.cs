using System.Text.Json;
using Notrelix.Application.Common.Events;

namespace Notrelix.Infrastructure.Data.Outbox;

public enum OutboxMessageType
{
    DomainEvent = 1,
    IntegrationEvent = 2,
}

public sealed class OutboxMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid? SourceEventId { get; private set; }
    public string MessageName { get; private set; } = null!;
    public int SchemaVersion { get; private set; }
    public OutboxMessageType MessageType { get; private set; }
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
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage From(IDomainEvent domainEvent, DateTimeOffset now, string messageName)
    {
        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            EventId = domainEvent.EventId,
            EventVersion = domainEvent.EventVersion,
            MessageName = messageName,
            SchemaVersion = 1,
            MessageType = OutboxMessageType.DomainEvent,
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

    public static OutboxMessage From(IIntegrationEvent integrationEvent, DateTimeOffset now)
    {
        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            EventId = integrationEvent.EventId,
            SourceEventId = integrationEvent.SourceEventId,
            EventVersion = integrationEvent.SchemaVersion,
            MessageName = integrationEvent.MessageName,
            SchemaVersion = integrationEvent.SchemaVersion,
            MessageType = OutboxMessageType.IntegrationEvent,
            WorkspaceId = integrationEvent.WorkspaceId,
            ActorUserId = integrationEvent.ActorUserId,
            CorrelationId = integrationEvent.CorrelationId,
            CausationId = integrationEvent.CausationId,
            PayloadJson = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions),
            Status = OutboxStatus.Pending,
            NextAttemptAt = now,
            CreatedAt = now,
        };
    }

    public void MarkProcessing(DateTimeOffset now)
    {
        Status = OutboxStatus.Processing;
        ProcessingStartedAt = now;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        Status = OutboxStatus.Processed;
        ProcessedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
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
            NextAttemptAt = now.AddSeconds(
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
    public const int ProcessingTimeoutSeconds = 60;
}

public static class OutboxStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
    public const string DeadLetter = "DeadLetter";
}
