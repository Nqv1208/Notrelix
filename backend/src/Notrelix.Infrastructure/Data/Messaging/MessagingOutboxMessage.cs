using System.Text.Json;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;

namespace Notrelix.Infrastructure.Data.Messaging;

public sealed class MessagingOutboxMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid? SourceEventId { get; private set; }
    public string SourceContext { get; private set; } = null!;
    public string MessageName { get; private set; } = null!;
    public int SchemaVersion { get; private set; } = 1;
    public string? Destination { get; private set; }
    public string ContentType { get; private set; } = "application/json";
    public string? SubjectType { get; private set; }
    public Guid? SubjectId { get; private set; }
    public string? AggregateType { get; private set; }
    public Guid? AggregateId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }
    public string? PartitionKey { get; private set; }
    public JsonDocument PayloadJson { get; private set; } = null!;
    public JsonDocument HeadersJson { get; private set; } = JsonDocument.Parse("{}");
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");
    public string Status { get; private set; } = "Pending";
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 5;
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private MessagingOutboxMessage() { }

    public MessagingOutboxMessage(
        Guid eventId,
        Guid? sourceEventId,
        string sourceContext,
        string messageName,
        int schemaVersion,
        string? destination,
        string? subjectType,
        Guid? subjectId,
        string? aggregateType,
        Guid? aggregateId,
        Guid? workspaceId,
        Guid? actorUserId,
        string? correlationId,
        string? causationId,
        string? partitionKey,
        JsonDocument payloadJson,
        JsonDocument? headersJson,
        JsonDocument? metadataJson,
        DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        SourceEventId = sourceEventId;
        SourceContext = sourceContext;
        MessageName = messageName;
        SchemaVersion = schemaVersion;
        Destination = destination;
        ContentType = "application/json";
        SubjectType = subjectType;
        SubjectId = subjectId;
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        WorkspaceId = workspaceId;
        ActorUserId = actorUserId;
        CorrelationId = correlationId;
        CausationId = causationId;
        PartitionKey = partitionKey;
        PayloadJson = payloadJson;
        HeadersJson = headersJson ?? JsonDocument.Parse("{}");
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
        NextAttemptAt = createdAt;
        CreatedAt = createdAt;
    }

    public void MarkProcessing(DateTimeOffset now)
    {
        Status = "Processing";
        ProcessingStartedAt = now;
        UpdatedAt = now;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        Status = "Processed";
        PublishedAt = now;
        ProcessedAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset now)
    {
        RetryCount++;
        LastErrorCode = errorCode;
        ErrorMessage = errorMessage;
        UpdatedAt = now;

        if (RetryCount >= MaxRetries)
        {
            Status = "DeadLetter";
        }
        else
        {
            Status = "Failed";
            NextAttemptAt = now.AddSeconds(
                Math.Min(Math.Pow(2, RetryCount), 60));
        }
    }

    public static MessagingOutboxMessage FromIntegrationEvent(
        IIntegrationEvent integrationEvent,
        IDomainEvent sourceDomainEvent,
        DateTimeOffset now)
    {
        var payloadJson = JsonSerializer.SerializeToDocument(integrationEvent, integrationEvent.GetType(), JsonOptions);
        return new MessagingOutboxMessage(
            eventId: integrationEvent.EventId,
            sourceEventId: integrationEvent.SourceEventId,
            sourceContext: sourceDomainEvent.SourceContext,
            messageName: integrationEvent.MessageName,
            schemaVersion: integrationEvent.SchemaVersion,
            destination: null,
            subjectType: sourceDomainEvent.SubjectType,
            subjectId: sourceDomainEvent.SubjectId,
            aggregateType: sourceDomainEvent.AggregateType,
            aggregateId: sourceDomainEvent.AggregateId,
            workspaceId: integrationEvent.WorkspaceId,
            actorUserId: integrationEvent.ActorUserId,
            correlationId: integrationEvent.CorrelationId,
            causationId: integrationEvent.CausationId,
            partitionKey: integrationEvent.WorkspaceId?.ToString(),
            payloadJson: payloadJson,
            headersJson: null,
            metadataJson: null,
            createdAt: now);
    }
}
