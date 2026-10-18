using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.MoveSpace;

/// <summary>
/// Moves a Space (with its subtree) under another parent Space or to the
/// Workspace root, at a neighbor-relative sibling position (WSP-021).
/// Authorized as <c>ManageSpaces</c> on the moved Space (pipeline) and on the
/// destination (<see cref="ISpaceTargetAuthorizer"/>, GOV-028); guarded by the
/// Space's expected version. Cycles are rejected by the Domain from the
/// destination's ancestry read under the hierarchy lock.
/// </summary>
public record MoveSpaceCommand(
    Guid WorkspaceId,
    Guid SpaceId,
    Guid? NewParentSpaceId,
    Guid? PreviousSiblingId,
    Guid? NextSiblingId,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageSpaces;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.space"), SpaceId, WorkspaceId);
}

public class MoveSpaceCommandHandler : IRequestHandler<MoveSpaceCommand, Result>
{
    private readonly ISpaceStore _spaces;
    private readonly ISpaceTargetAuthorizer _targetAuthorizer;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MoveSpaceCommandHandler(
        ISpaceStore spaces,
        ISpaceTargetAuthorizer targetAuthorizer,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _spaces = spaces;
        _targetAuthorizer = targetAuthorizer;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MoveSpaceCommand request, CancellationToken ct)
    {
        var space = await _spaces.FindAsync(request.WorkspaceId, request.SpaceId, ct)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);

        if (request.NewParentSpaceId != space.ParentSpaceId)
            await _targetAuthorizer.AuthorizePlacementAsync(request.WorkspaceId, request.NewParentSpaceId, ct);

        await _spaces.LockHierarchyAsync(request.WorkspaceId, ct);

        SpaceParentFact? parent = null;
        if (request.NewParentSpaceId is { } parentId)
        {
            parent = await _spaces.FindParentFactAsync(parentId, ct);
            if (parent is null || parent.ParentWorkspaceId != request.WorkspaceId)
                throw new NotFoundException(nameof(Space), parentId);
        }

        var siblings = await _spaces.LoadSiblingsAsync(request.WorkspaceId, request.NewParentSpaceId, ct);
        var position = SpacePlacementResolver.Place(
            siblings,
            request.PreviousSiblingId,
            request.NextSiblingId,
            movedSpaceId: space.Id);
        var siblingNames = siblings.Where(s => s.Id != space.Id).Select(s => s.Name).ToList();

        var parentChanged = space.ParentSpaceId != request.NewParentSpaceId;
        space.Move(new SpacePlacement(parent, position, siblingNames), _requestContext.UserId, _dateTimeProvider.UtcNow);

        if (parentChanged)
            await _spaces.StageMovedAsync(space, ct);

        return Result.Success();
    }
}
