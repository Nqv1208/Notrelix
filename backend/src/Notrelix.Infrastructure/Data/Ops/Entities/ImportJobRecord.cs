namespace Notrelix.Infrastructure.Data.Ops.Entities;

public sealed class ImportJobRecord
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string JobType { get; private set; } = null!;
    public string? TargetResourceType { get; private set; }
    public Guid? TargetResourceId { get; private set; }
    public Guid? SourceFileAttachmentId { get; private set; }
    public string Status { get; private set; } = null!;
    public int TotalRecords { get; private set; }
    public int ProcessedRecords { get; private set; }
    public int SucceededRecords { get; private set; }
    public int FailedRecords { get; private set; }
    public string OptionsJson { get; private set; } = null!;
    public string? ResultJson { get; private set; }
    public string? ErrorSummary { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? ErrorFileAttachmentId { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private ImportJobRecord() { }

    public static ImportJobRecord Create(
        Guid id,
        Guid workspaceId,
        string jobType,
        string status,
        string optionsJson,
        DateTimeOffset createdAt)
    {
        return new ImportJobRecord
        {
            Id = id,
            WorkspaceId = workspaceId,
            JobType = jobType,
            Status = status,
            TotalRecords = 0,
            ProcessedRecords = 0,
            SucceededRecords = 0,
            FailedRecords = 0,
            OptionsJson = optionsJson,
            CreatedAt = createdAt,
        };
    }
}
