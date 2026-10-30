using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Services;

namespace Notrelix.Application.Features.Documents.Pages.Commands.RestorePage;

/// <summary>
/// Takes a page out of the trash with the pages that went to the trash together with it (the same archive
/// time); a page trashed on its own earlier stays in the trash. Same authority as moving it there
/// (<c>ArchivePage</c>, Editor level on the page), and the caller must be allowed to restore every page the
/// cascade changes: one refused page refuses the whole operation. Restore validates today's constraints: a
/// parent that is trashed or gone brings the page back at the root of its Space, and a Space that is archived
/// refuses the restore. Restoring an active page is a no-op.
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
    private readonly IPageSubtreeAuthorizer _subtree;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestorePageCommandHandler(
        IPageSubtreeStore pages,
        IPageSpacePlacement placement,
        IPageSubtreeAuthorizer subtree,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _pages = pages;
        _placement = placement;
        _subtree = subtree;
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

        // One archive time marks what went to the trash together (the cascade wrote a single `now`).
        var archivedAt = page.UpdatedAt;
        var descendants = await _pages.LoadDescendantsAsync(workspaceId, page.Id, ct);
        var together = descendants
            .Where(descendant => archivedAt is not null && !descendant.IsDeleted && descendant.Status == PageStatus.Archived && descendant.UpdatedAt == archivedAt)
            .ToDictionary(descendant => descendant.Id);
        await _subtree.EnsureAllowedAsync(together.Keys, id => new RestorePageCommand(id), ct);

        page.Unarchive(userId, now, detach);

        // Walked parents first: a page whose parent stays in the trash (or was deleted) has nowhere to hang
        // and returns at the root of its Space.
        var live = new HashSet<Guid> { page.Id };
        foreach (var descendant in descendants)
        {
            if (descendant.IsDeleted) continue;
            if (descendant.Status == PageStatus.Active)
            {
                live.Add(descendant.Id);
                continue;
            }
            if (!together.ContainsKey(descendant.Id)) continue;
            descendant.Unarchive(userId, now, detachFromParent: descendant.ParentId is not { } parentOf || !live.Contains(parentOf));
            live.Add(descendant.Id);
        }

        return Result.Success();
    }
}
