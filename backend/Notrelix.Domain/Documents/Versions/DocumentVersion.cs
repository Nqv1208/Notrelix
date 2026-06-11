using Notrelix.Domain.Common;

namespace Notrelix.Domain.Documents.Versions;



public class DocumentVersion : AggregateRoot
{
    public Guid PageId { get; private set; }
    public int VersionNumber { get; private set; }
    public DocumentSnapshot Snapshot { get; private set; } = null!;
    public string? ChangeSummary { get; private set; }

    private DocumentVersion() : base() { }

    public static DocumentVersion Create(Guid pageId, int versionNumber, DocumentSnapshot snapshot, Guid createdBy, DateTimeOffset createdAt, string? changeSummary = null)
    {
        Guard.NotEmpty(pageId);
        Guard.Positive(versionNumber);
        Guard.NotNull(snapshot);

        var version = new DocumentVersion
        {
            PageId = pageId,
            VersionNumber = versionNumber,
            Snapshot = snapshot,
            ChangeSummary = changeSummary?.Trim()
        };

        version.SetAuditOnCreate(createdBy, createdAt);
        return version;
    }
}
