namespace Notrelix.Infrastructure.Data.Ops.Entities;

public sealed class ExportJobRecord
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string JobType { get; private set; } = null!;
    public string? SourceResourceType { get; private set; }
    public Guid? SourceResourceId { get; private set; }
    public string Status { get; private set; } = null!;
    public string Format { get; private set; } = null!;
    public int? RowCount { get; private set; }
    public string OptionsJson { get; private set; } = null!;
    public string FiltersJson { get; private set; } = null!;
    public Guid? ResultAttachmentId { get; private set; }
    public Guid? ResultFileId { get; private set; }
    public string? StorageProvider { get; private set; }
    public string? StorageKey { get; private set; }
    public string? DownloadUrl { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

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
