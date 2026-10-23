using Notrelix.Application.Features.Governance.Public.Access;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Reads the title of the Board or Page a resource invitation names, in the
/// request tenant (WSP-026). A resource of another Workspace is unavailable.
/// </summary>
public sealed class GuestResourceInvitationFactsAdapter : IGuestResourceInvitationFacts
{
    private readonly ApplicationDbContext _db;

    public GuestResourceInvitationFactsAdapter(ApplicationDbContext db) => _db = db;

    public Task<string?> ResolveTitleAsync(
        string resourceKind,
        Guid resourceId,
        Guid workspaceId,
        CancellationToken cancellationToken) =>
        resourceKind switch
        {
            "work-management.board" => _db.Boards.AsNoTracking()
                .Where(b => b.Id == resourceId && b.WorkspaceId == workspaceId && !b.IsArchived && b.DeletedAt == null)
                .Select(b => (string?)b.Title)
                .FirstOrDefaultAsync(cancellationToken),
            "documents.page" => _db.Pages.AsNoTracking()
                .Where(p => p.Id == resourceId && p.WorkspaceId == workspaceId
                            && p.Status == Domain.Documents.Pages.PageStatus.Active && p.DeletedAt == null)
                .Select(p => (string?)p.Title)
                .FirstOrDefaultAsync(cancellationToken),
            _ => Task.FromResult<string?>(null)
        };
}
