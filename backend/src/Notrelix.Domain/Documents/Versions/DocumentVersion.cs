using Notrelix.Domain.Documents.Versions.Events;
namespace Notrelix.Domain.Documents.Versions;

public class DocumentVersion : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid PageId { get; private set; }
    public int VersionNumber { get; private set; }
    public DocumentSnapshot Snapshot { get; private set; } = null!;
    /// <summary>The name of a named version, "Restored from version N" on a restore; empty for an automatic one.</summary>
    public string? ChangeSummary { get; private set; }
    public DocumentVersionKind Kind { get; private set; }

    public const int MaxNameLength = 200;

    private DocumentVersion() : base() { }

    public static DocumentVersion Create(Guid accountId, Guid workspaceId, Guid pageId, int versionNumber, DocumentSnapshot snapshot, Guid createdBy, DateTimeOffset createdAt, string? changeSummary = null, DocumentVersionKind kind = DocumentVersionKind.Automatic)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(pageId);
        Guard.Positive(versionNumber);
        Guard.NotNull(snapshot);
        if (changeSummary is { Length: > MaxNameLength } && changeSummary.Trim().Length > MaxNameLength)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Version_NameTooLong, $"A version name is at most {MaxNameLength} characters.");
        if (kind == DocumentVersionKind.Named && string.IsNullOrWhiteSpace(changeSummary))
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Version_NameRequired, "A named version needs a name.");

        var version = new DocumentVersion
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            PageId = pageId,
            VersionNumber = versionNumber,
            Snapshot = snapshot,
            ChangeSummary = string.IsNullOrWhiteSpace(changeSummary) ? null : changeSummary.Trim(),
            Kind = kind
        };

        version.SetAuditOnCreate(createdBy, createdAt);
        version.RaiseDomainEvent(new DocumentVersionCreatedDomainEvent(accountId, workspaceId, pageId, versionNumber, createdAt));
        return version;
    }

    public void ApplyRestore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        var pending = PrepareAuditUpdate(restoredBy, restoredAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new DocumentVersionRestoredDomainEvent(AccountId, WorkspaceId, PageId, VersionNumber, restoredAt));
    }
}
