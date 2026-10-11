using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.RestoreSpace;

public record RestoreSpaceCommand(
    Guid WorkspaceId,
    Guid SpaceId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class RestoreSpaceCommandHandler : IRequestHandler<RestoreSpaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceStore _spaces;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreSpaceCommandHandler(IWorkspaceDbContext context, ISpaceStore spaces, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _spaces = spaces;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreSpaceCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        if (!space.IsDeleted)
            return Result.Success();

        // WSP-021/WSP-022: the parent must be active, the name unique among the
        // current siblings, and the old sibling position is reused only if free.
        await _spaces.LockHierarchyAsync(request.WorkspaceId, ct);
        SpaceParentFact? parent = null;
        if (space.ParentSpaceId is { } parentId)
            parent = await _spaces.FindParentFactAsync(parentId, ct)
                ?? new SpaceParentFact(parentId, request.WorkspaceId, IsActive: false, []);

        var siblings = (await _spaces.LoadSiblingsAsync(request.WorkspaceId, space.ParentSpaceId, ct))
            .Where(s => s.Id != space.Id)
            .ToList();
        var position = siblings.Any(s => s.Position.Equals(space.Position))
            ? SpacePlacementResolver.Place(siblings, null, null)
            : space.Position;

        space.Restore(
            _requestContext.UserId,
            _dateTimeProvider.UtcNow,
            new SpacePlacement(parent, position, siblings.Select(s => s.Name).ToList()));
        return Result.Success();
    }
}
