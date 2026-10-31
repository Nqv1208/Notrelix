namespace Notrelix.Infrastructure.Data.Ops.Entities;

public sealed class ExportJobRecord
{
    public Guid Id { get; internal set; }
    public Guid WorkspaceId { get; internal set; }
    public string JobType { get; internal set; } = null!;
    public string? SourceResourceType { get; internal set; }
    public Guid? SourceResourceId { get; internal set; }
    public string Status { get; internal set; } = null!;
    public string Format { get; internal set; } = null!;
    public int? RowCount { get; internal set; }
    public string OptionsJson { get; internal set; } = null!;
    public string FiltersJson { get; internal set; } = null!;
    public Guid? ResultAttachmentId { get; internal set; }
    public Guid? ResultFileId { get; internal set; }
    public string? StorageProvider { get; internal set; }
    public string? StorageKey { get; internal set; }
    public string? DownloadUrl { get; internal set; }
    public DateTimeOffset? ExpiresAt { get; internal set; }
    public string? ErrorMessage { get; internal set; }
    public Guid? RequestedByUserId { get; internal set; }
    public DateTimeOffset? StartedAt { get; internal set; }
    public DateTimeOffset? CompletedAt { get; internal set; }
    public DateTimeOffset CreatedAt { get; internal set; }
    public DateTimeOffset? UpdatedAt { get; internal set; }

    private ExportJobRecord() { }

    public static ExportJobRecord Create(
        Guid id,
        Guid workspaceId,
        string jobType,
        string status,
        string format,
        string optionsJson,
        string filtersJson,
        DateTimeOffset createdAt)
    {
        return new ExportJobRecord
        {
            Id = id,
            WorkspaceId = workspaceId,
            JobType = jobType,
            Status = status,
            Format = format,
            OptionsJson = optionsJson,
            FiltersJson = filtersJson,
            CreatedAt = createdAt,
        };
    }
}
