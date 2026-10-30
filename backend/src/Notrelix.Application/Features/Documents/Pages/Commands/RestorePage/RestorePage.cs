using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Services;

namespace Notrelix.Application.Features.Documents.Pages.Commands.RestorePage;

/// <summary>
/// Takes a page out of the trash with the pages that went to the trash under it. Same authority as moving it
/// there (<c>ArchivePage</c>, Editor level on the page). Restore validates today's constraints: a parent that
/// is trashed or gone brings the page back at the root of its Space, and a Space that is archived refuses the
/// restore. Restoring an active page is a no-op.
/// </summary>
public record RestorePageCommand(Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ArchivePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class RestorePageCommandHandler : IRequestHandler<RestorePageCommand, Result>
{
    private readonly IPageSubtreeStore _pages;
    private readonly IPageSpacePlacement _placement;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestorePageCommandHandler(
        IPageSubtreeStore pages,
        IPageSpacePlacement placement,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _pages = pages;
        _placement = placement;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestorePageCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var page = await _pages.FindAsync(workspaceId, request.PageId, ct);
        if (page is null || page.IsDeleted) throw new NotFoundException(nameof(Page), request.PageId);

        // Page.Unarchive owns the lifecycle: an active page is a semantic no-op.
        if (page.Status == PageStatus.Active) return Result.Success();

        if (page.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(workspaceId, spaceId, ct);

        var detach = false;
        if (page.ParentId is { } parentId)
        {
            var parent = await _pages.FindAsync(workspaceId, parentId, ct);
            detach = parent is null || parent.IsDeleted || parent.Status == PageStatus.Archived;
        }

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;
        page.Unarchive(userId, now, detach);

        // The pages trashed under it come back with it, under their own parents; one whose parent was
        // deleted for good has nowhere to hang and returns at the root.
        var deleted = new HashSet<Guid>();
        foreach (var descendant in await _pages.LoadDescendantsAsync(workspaceId, page.Id, ct))
        {
            if (descendant.IsDeleted) { deleted.Add(descendant.Id); continue; }
            descendant.Unarchive(userId, now, detachFromParent: descendant.ParentId is { } parent && deleted.Contains(parent));
        }

        return Result.Success();
    }
}
