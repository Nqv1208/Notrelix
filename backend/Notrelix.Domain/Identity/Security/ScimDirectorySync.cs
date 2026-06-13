using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public enum ScimSyncStatus
{
    Enabled,
    Paused,
    Disabled,
    Deleted
}

public class ScimDirectorySync : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string ProviderName { get; private set; } = null!;
    public ScimSyncStatus Status { get; private set; } = ScimSyncStatus.Enabled;
    public DateTimeOffset? LastSyncAt { get; private set; }
    public string CursorJson { get; private set; } = "{}";
    public string ConfigJson { get; private set; } = "{}";

    private ScimDirectorySync() : base() { }

    public static ScimDirectorySync Create(
        Guid workspaceId,
        string providerName,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? configJson = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(providerName);

        var sync = new ScimDirectorySync
        {
            WorkspaceId = workspaceId,
            ProviderName = providerName.Trim(),
            Status = ScimSyncStatus.Enabled,
            ConfigJson = configJson ?? "{}"
        };

        sync.SetAuditOnCreate(createdBy, createdAt);
        return sync;
    }

    public void Pause(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Status = ScimSyncStatus.Paused;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Resume(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Status = ScimSyncStatus.Enabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void RecordSync(string cursorJson, DateTimeOffset syncAt)
    {
        EnsureNotDeleted();
        LastSyncAt = syncAt;
        CursorJson = cursorJson ?? "{}";
        AddDomainEvent(new ScimSyncCompletedDomainEvent(WorkspaceId, Id, syncAt));
        IncrementVersion();
    }
}
