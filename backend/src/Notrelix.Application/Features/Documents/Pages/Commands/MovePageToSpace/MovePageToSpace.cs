using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Services;

namespace Notrelix.Application.Features.Documents.Pages.Commands.MovePageToSpace;

/// <summary>
/// Moves a root page — with its whole page subtree — into a Workspaces Space, or
/// back to the Workspace root (<paramref name="SpaceId"/> null). A nested page
/// follows its root page and is rejected by the Domain. Authorized on the
/// source page (<c>UpdatePage</c>, pipeline) and on the target (the create-a-
/// root-page right there, <see cref="IPageSpacePlacement"/>; GOV-028); guarded by
/// the root page's expected version. Every page below the root changes Space with it, so the caller must be
/// allowed to edit each of them (<see cref="IPageSubtreeAuthorizer"/>): one refused page refuses the whole move.
/// The same Space is a no-op.
/// </summary>
public record MovePageToSpaceCommand(
    Guid PageId,
    Guid? SpaceId,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class MovePageToSpaceCommandHandler : IRequestHandler<MovePageToSpaceCommand, Result>
{
    private readonly IPageSubtreeStore _pages;
    private readonly IPageSpacePlacement _placement;
    private readonly IPageSubtreeAuthorizer _subtree;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MovePageToSpaceCommandHandler(
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

    public async Task<Result> Handle(MovePageToSpaceCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        await _pages.LockTreeAsync(workspaceId, ct);
        var page = await _pages.FindAsync(workspaceId, request.PageId, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);

        if (page.SpaceId == request.SpaceId && page.ParentId is null)
            return Result.Success();

        await _placement.AuthorizeTargetAsync(workspaceId, request.SpaceId, ct);
        if (request.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(workspaceId, spaceId, ct);

        var descendants = (await _pages.LoadDescendantsAsync(workspaceId, page.Id, ct))
            .Where(descendant => !descendant.IsDeleted)
            .ToList();
        await _subtree.EnsureAllowedAsync(descendants.Select(descendant => descendant.Id), id => new MovePageToSpaceCommand(id, request.SpaceId, 1), ct);

        var now = _dateTimeProvider.UtcNow;
        page.PlaceInSpace(request.SpaceId, _requestContext.UserId, now);
        foreach (var descendant in descendants)
            descendant.FollowRootSpace(request.SpaceId, _requestContext.UserId, now);

        return Result.Success();
    }
}
