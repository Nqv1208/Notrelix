using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;

/// <summary>
/// Creates a Space at the Workspace root or under a parent Space (WSP-021).
/// Authorized as <c>ManageSpaces</c> on the parent Space, or on the Workspace
/// for a root Space (GOV-028). Visibility defaults to Private for a Department
/// Space and to Workspace (open to the parent's audience) otherwise (WSP-023).
/// Placement is neighbor-relative among the parent's children; both neighbors
/// absent appends.
/// </summary>
public record CreateSpaceCommand(
    Guid WorkspaceId,
    string Name,
    string? Visibility = null,
    string? Description = null,
    string? SpaceType = null,
    Guid? ParentSpaceId = null,
    Guid? PreviousSiblingId = null,
    Guid? NextSiblingId = null
) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail
{
    public PermissionAction Action => PermissionAction.ManageSpaces;

    public ResourceRef Resource => ParentSpaceId is { } parentId
        ? ResourceRef.Create(ResourceKind.Create("workspaces.space"), parentId, WorkspaceId)
        : ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CreateSpaceCommandHandler : IRequestHandler<CreateSpaceCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceStore _spaces;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSpaceCommandHandler(
        IWorkspaceDbContext context,
        ISpaceStore spaces,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _spaces = spaces;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var spaceType = request.SpaceType is null
            ? Domain.Workspaces.Spaces.SpaceType.Folder
            : Enum.Parse<SpaceType>(request.SpaceType, ignoreCase: true);
        var visibility = request.Visibility is null
            ? (spaceType == Domain.Workspaces.Spaces.SpaceType.Department ? SpaceVisibility.Private : SpaceVisibility.Workspace)
            : Enum.Parse<SpaceVisibility>(request.Visibility, ignoreCase: true);

        await _spaces.LockHierarchyAsync(request.WorkspaceId, ct);

        SpaceParentFact? parent = null;
        if (request.ParentSpaceId is { } parentId)
        {
            parent = await _spaces.FindParentFactAsync(parentId, ct);
            if (parent is null || parent.ParentWorkspaceId != request.WorkspaceId)
                throw new NotFoundException(nameof(Space), parentId);
        }

        var siblings = await _spaces.LoadSiblingsAsync(request.WorkspaceId, request.ParentSpaceId, ct);
        var position = SpacePlacementResolver.Place(siblings, request.PreviousSiblingId, request.NextSiblingId);
        var placement = new SpacePlacement(parent, position, siblings.Select(s => s.Name).ToList());

        var space = Space.Create(
            workspace.AccountId,
            request.WorkspaceId,
            request.Name,
            visibility,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow,
            spaceType,
            request.Description,
            placement);

        _spaces.Add(space);
        await _spaces.StageCreatedAsync(space, ct);
        return Result<Guid>.Success(space.Id);
    }
}
