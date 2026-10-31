using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Governance public target action for a resource invitation's explicit entry
/// (WSP-026 / GOV-030, view-only). Runs on the request's unit of work, so the entry is
/// saved with the Workspace membership or not at all. Resource existence is
/// checked in the request tenant, so a resource of another Workspace is
/// unavailable (cross-tenant fail closed).
/// </summary>
public sealed class GuestResourceAccessActionsAdapter : IGuestResourceAccessActions
{
    private readonly ApplicationDbContext _db;

    public GuestResourceAccessActionsAdapter(ApplicationDbContext db) => _db = db;

    public async Task<GuestResourceEntryOutcome> EnsureGuestEntryAsync(
        GuestResourceEntryRequest request,
        CancellationToken cancellationToken)
    {
        Guard.NotEmpty(request.GuestUserId);

        if (!Enum.TryParse<PermissionLevel>(request.Level, ignoreCase: true, out var level)
            || level is not (PermissionLevel.Viewer or PermissionLevel.Commenter or PermissionLevel.Editor))
        {
            throw new ArgumentOutOfRangeException(nameof(request), "A guest resource entry is view-only.");
        }

        // A guest is view-only (GOV-030): an invitation created before that cap, with a higher stored level,
        // still opens the resource for reading and never writes a higher entry.
        level = PermissionLevel.Viewer;

        var available = request.ResourceKind switch
        {
            "work-management.board" => await _db.Boards.AsNoTracking().AnyAsync(
                b => b.Id == request.ResourceId && b.WorkspaceId == request.WorkspaceId
                     && !b.IsArchived && b.DeletedAt == null, cancellationToken),
            "documents.page" => await _db.Pages.AsNoTracking().AnyAsync(
                p => p.Id == request.ResourceId && p.WorkspaceId == request.WorkspaceId
                     && p.Status == Domain.Documents.Pages.PageStatus.Active && p.DeletedAt == null, cancellationToken),
            _ => false
        };

        if (!available) return GuestResourceEntryOutcome.ResourceUnavailable;

        var kind = ResourceKind.Create(request.ResourceKind);
        var existing = await _db.ResourcePermissions.FirstOrDefaultAsync(
            p => p.WorkspaceId == request.WorkspaceId
                 && p.ResourceKind == kind
                 && p.ResourceId == request.ResourceId
                 && p.DeletedAt == null
                 && p.SubjectType == PermissionSubjectType.User
                 && p.SubjectId == request.GuestUserId, cancellationToken);

        if (existing is null)
        {
            _db.ResourcePermissions.Add(ResourcePermission.Grant(
                request.AccountId, request.WorkspaceId, kind, request.ResourceId,
                PermissionSubjectType.User, request.GuestUserId, level,
                request.GrantedBy, request.Now));
            return GuestResourceEntryOutcome.Created;
        }

        if (existing.Level >= level) return GuestResourceEntryOutcome.Unchanged;

        existing.ChangeLevel(level, request.GrantedBy, request.Now);
        return GuestResourceEntryOutcome.Raised;
    }
}
