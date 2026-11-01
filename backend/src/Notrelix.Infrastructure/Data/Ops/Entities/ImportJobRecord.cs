namespace Notrelix.Infrastructure.Data.Ops.Entities;

public sealed class ImportJobRecord
{
    public Guid Id { get; internal set; }
    public Guid WorkspaceId { get; internal set; }
    public string JobType { get; internal set; } = null!;
    public string? TargetResourceType { get; internal set; }
    public Guid? TargetResourceId { get; internal set; }
    public Guid? SourceFileAttachmentId { get; internal set; }
    public string Status { get; internal set; } = null!;
    public int TotalRecords { get; internal set; }
    public int ProcessedRecords { get; internal set; }
    public int SucceededRecords { get; internal set; }
    public int FailedRecords { get; internal set; }
    public string OptionsJson { get; internal set; } = null!;
    public string? ResultJson { get; internal set; }
    public string? ErrorSummary { get; internal set; }
    public string? ErrorMessage { get; internal set; }
    public Guid? ErrorFileAttachmentId { get; internal set; }
    public Guid? RequestedByUserId { get; internal set; }
    public DateTimeOffset? StartedAt { get; internal set; }
    public DateTimeOffset? CompletedAt { get; internal set; }
    public DateTimeOffset? CancelledAt { get; internal set; }
    public DateTimeOffset? ExpiresAt { get; internal set; }
    public DateTimeOffset CreatedAt { get; internal set; }
    public DateTimeOffset? UpdatedAt { get; internal set; }

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
