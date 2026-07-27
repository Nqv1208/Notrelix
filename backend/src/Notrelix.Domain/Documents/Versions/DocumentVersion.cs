using Notrelix.Domain.Documents.Versions.Events;
namespace Notrelix.Domain.Documents.Versions;

public class DocumentVersion : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid PageId { get; private set; }
    public int VersionNumber { get; private set; }
    public DocumentSnapshot Snapshot { get; private set; } = null!;
    public string? ChangeSummary { get; private set; }

    private DocumentVersion() : base() { }

    public static DocumentVersion Create(Guid accountId, Guid workspaceId, Guid pageId, int versionNumber, DocumentSnapshot snapshot, Guid createdBy, DateTimeOffset createdAt, string? changeSummary = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(pageId);
        Guard.Positive(versionNumber);
        Guard.NotNull(snapshot);

        var version = new DocumentVersion
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            PageId = pageId,
            VersionNumber = versionNumber,
            Snapshot = snapshot,
            ChangeSummary = changeSummary?.Trim()
        };

        version.SetAuditOnCreate(createdBy, createdAt);
        version.RaiseDomainEvent(new DocumentVersionCreatedDomainEvent(accountId, workspaceId, pageId, versionNumber, createdAt));
        return version;
    }

    public void ApplyRestore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new DocumentVersionRestoredDomainEvent(AccountId, WorkspaceId, PageId, VersionNumber, restoredAt));
    }
}
