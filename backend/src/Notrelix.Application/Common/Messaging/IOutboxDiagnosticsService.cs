namespace Notrelix.Application.Common.Messaging;

public interface IOutboxDiagnosticsService
{
    Task<OutboxStatsResult> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<List<OutboxMessageResult>> GetPendingAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<List<OutboxMessageResult>> GetFailedAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<OutboxMessageDetailResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record OutboxStatsResult(
    int Total,
    Dictionary<string, int> ByStatus,
    DateTimeOffset? OldestPending);

public sealed record OutboxMessageResult(
    Guid Id,
    string MessageName,
    string MessageType,
    string Status,
    int RetryCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset? ProcessingStartedAt,
    Guid? WorkspaceId);

public sealed record OutboxMessageDetailResult(
    Guid Id,
    Guid EventId,
    Guid? SourceEventId,
    string MessageName,
    int? SchemaVersion,
    string MessageType,
    int? EventVersion,
    string Status,
    int RetryCount,
    int? MaxRetries,
    DateTimeOffset CreatedAt,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? ProcessedAt,
    string? Error,
    Guid? WorkspaceId,
    Guid? ActorUserId,
    string? CorrelationId,
    string? CausationId,
    string? PayloadJson);
