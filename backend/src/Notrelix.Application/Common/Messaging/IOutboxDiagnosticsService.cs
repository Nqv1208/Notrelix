namespace Notrelix.Application.Common.Messaging;

/// <summary>Operator reads are bounded: a requested limit is clamped to 1 to 200 (Lead decision L9, UC-GOV-H11).</summary>
public static class OutboxDiagnosticsLimits
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public static int Clamp(int limit) => Math.Clamp(limit, 1, MaxLimit);
}

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

/// <remarks>
/// Metadata only. The message payload is never part of this result (Lead decision L9: operator reads return
/// no payload), so a payload body or a payload-derived secret cannot reach the operator through this route.
/// </remarks>
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
    string? CausationId);
