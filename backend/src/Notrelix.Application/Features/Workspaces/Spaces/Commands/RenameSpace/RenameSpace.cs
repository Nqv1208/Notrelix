using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.RenameSpace;

public record RenameSpaceCommand(
    Guid WorkspaceId,
    Guid SpaceId,
    string Name
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    // GOV-028: ManageSpaces on the Space itself (workspace Owner/Admin, or an
    // effective Space Admin of the Space or one of its ancestors).
    public PermissionAction Action => PermissionAction.ManageSpaces;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.space"), SpaceId, WorkspaceId);
}

public class RenameSpaceCommandHandler : IRequestHandler<RenameSpaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceStore _spaces;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RenameSpaceCommandHandler(IWorkspaceDbContext context, ISpaceStore spaces, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _spaces = spaces;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RenameSpaceCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        // WSP-021: sibling names are unique; read the siblings under the hierarchy lock.
        await _spaces.LockHierarchyAsync(request.WorkspaceId, ct);
        var siblings = await _spaces.LoadSiblingsAsync(request.WorkspaceId, space.ParentSpaceId, ct);
        var siblingNames = siblings.Where(s => s.Id != space.Id).Select(s => s.Name).ToList();

        space.Rename(request.Name, _requestContext.UserId, _dateTimeProvider.UtcNow, siblingNames);
        return Result.Success();
    }
}
