namespace Notrelix.Infrastructure.Data.Projections.Search;

public sealed class SearchIndexJobRecord
{
    public Guid Id { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public string ResourceKind { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public string Operation { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public int Priority { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public Guid? CausationId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string MetadataJson { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    private SearchIndexJobRecord() { }

    public static SearchIndexJobRecord Create(
        Guid id,
        Guid? workspaceId,
        string resourceType,
        Guid resourceId,
        string operation,
        string status,
        int priority,
        int maxAttempts,
        DateTimeOffset availableAt,
        Guid? correlationId,
        Guid? causationId,
        string metadataJson,
        DateTimeOffset createdAt)
    {
        return new SearchIndexJobRecord
        {
            Id = id,
            WorkspaceId = workspaceId,
            ResourceKind = resourceType,
            ResourceId = resourceId,
            Operation = operation,
            Status = status,
            Priority = priority,
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            AvailableAt = availableAt,
            CorrelationId = correlationId,
            CausationId = causationId,
            MetadataJson = metadataJson,
            CreatedAt = createdAt,
        };
    }
}
