using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Application.Features.Documents.Pages.Commands.MovePage;

/// <summary>
/// Puts a page, with its whole subtree, under another parent page, or makes it a root page of its Space
/// (<paramref name="NewParentId"/> null). Authorized as an edit of the page (<c>UpdatePage</c>, pipeline) and as
/// creating a page at the destination (under the new parent, or in the Space for a root page); guarded by the
/// page's expected version. The destination must be an active page of the same workspace and may not be the page
/// itself or one of its descendants; the page and its deepest descendant must stay within the nesting limit.
/// The subtree follows the Space of the new parent, and when that changes the Space every page in it must be
/// one the caller may edit. Moving to the place the page already has is a no-op.
/// </summary>
public record MovePageCommand(
    Guid PageId,
    Guid? NewParentId,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class MovePageCommandHandler : IRequestHandler<MovePageCommand, Result>
{
    private readonly IPageSubtreeStore _pages;
    private readonly IPageSpacePlacement _placement;
    private readonly IPageSubtreeAuthorizer _subtree;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MovePageCommandHandler(
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

    public async Task<Result> Handle(MovePageCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        await _pages.LockTreeAsync(workspaceId, ct);
        var page = await _pages.FindAsync(workspaceId, request.PageId, ct);
        if (page is null || page.IsDeleted) throw new NotFoundException(nameof(Page), request.PageId);

        // Page.Move owns the lifecycle: the same parent is a semantic no-op (no version bump, no event).
        if (page.ParentId == request.NewParentId) return Result.Success();

        var newSpaceId = page.SpaceId;
        var ancestry = new Dictionary<Guid, Guid?>();
        var parentDepth = 0;
        if (request.NewParentId is { } parentId)
        {
            var parent = await _pages.FindAsync(workspaceId, parentId, ct);
            if (parent is null || parent.IsDeleted || parent.Status == PageStatus.Archived)
                throw new NotFoundException(nameof(Page), parentId);

            await _placement.AuthorizeParentAsync(workspaceId, parentId, ct);
            // A page cannot be moved into an archived Space (the same rule creating or restoring a page there has).
            if (parent.SpaceId is { } parentSpace && parent.SpaceId != page.SpaceId)
                await _placement.EnsureActiveTargetAsync(workspaceId, parentSpace, ct);
            foreach (var (id, above) in await _pages.LoadAncestryAsync(workspaceId, parentId, ct)) ancestry[id] = above;
            parentDepth = ancestry.Count;
            newSpaceId = parent.SpaceId;
            PageTreeRules.EnsureNoCycle(page.Id, parentId, id => ancestry.GetValueOrDefault(id));
        }
        else
        {
            // A new root page of the Space it is already in.
            await _placement.AuthorizeTargetAsync(workspaceId, page.SpaceId, ct);
        }

        // Moving into another Space changes who can see the pages below: each must be the caller's to edit, and
        // each follows the Space, so they are loaded for writing. Within one Space only the depth of the
        // subtree is needed, and it is read without loading the pages.
        var spaceChanged = newSpaceId != page.SpaceId;
        var descendants = new List<Page>();
        if (spaceChanged)
        {
            descendants = (await _pages.LoadDescendantsAsync(workspaceId, page.Id, ct))
                .Where(descendant => !descendant.IsDeleted)
                .ToList();
            PageTreeRules.EnsureCanNestUnder(parentDepth, SubtreeDepth(page, descendants));
            await _subtree.EnsureAllowedAsync(descendants.Select(descendant => descendant.Id), id => new MovePageCommand(id, null, 1), ct);
        }
        else
        {
            PageTreeRules.EnsureCanNestUnder(parentDepth, await _pages.SubtreeDepthAsync(workspaceId, page.Id, ct));
        }

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;
        page.Move(request.NewParentId, newSpaceId, userId, now, id => ancestry.GetValueOrDefault(id));
        if (spaceChanged)
            foreach (var descendant in descendants) descendant.FollowRootSpace(newSpaceId, userId, now);

        return Result.Success();
    }

    /// <summary>Levels from the page down to its deepest descendant, the page itself counting as 1.</summary>
    private static int SubtreeDepth(Page page, IEnumerable<Page> descendants)
    {
        var level = new Dictionary<Guid, int> { [page.Id] = 1 };
        // Parents come before their children (the store walks the tree level by level).
        foreach (var descendant in descendants)
            level[descendant.Id] = (descendant.ParentId is { } parent ? level.GetValueOrDefault(parent, 1) : 1) + 1;
        return level.Values.Max();
    }
}
