using Notrelix.Domain.Identity.Security.Events;
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
        sync.AddDomainEvent(new ScimDirectorySyncCreatedDomainEvent(workspaceId, sync.Id, sync.ProviderName, createdBy, createdAt));
        return sync;
    }

    public void Pause(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == ScimSyncStatus.Paused) return;
        Status = ScimSyncStatus.Paused;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new ScimDirectorySyncPausedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Resume(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == ScimSyncStatus.Enabled) return;
        Status = ScimSyncStatus.Enabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new ScimDirectorySyncResumedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void RecordSync(string cursorJson, DateTimeOffset syncAt)
    {
        EnsureNotDeleted();
        LastSyncAt = syncAt;
        CursorJson = cursorJson ?? "{}";
        AddDomainEvent(new ScimDirectorySyncCompletedDomainEvent(WorkspaceId, Id, syncAt));
        IncrementVersion();
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new ScimDirectorySyncSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new ScimDirectorySyncRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
