using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.UnarchiveSpace;

public record UnarchiveSpaceCommand(
    Guid WorkspaceId,
    Guid SpaceId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UnarchiveSpaceCommandHandler : IRequestHandler<UnarchiveSpaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceStore _spaces;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UnarchiveSpaceCommandHandler(IWorkspaceDbContext context, ISpaceStore spaces, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _spaces = spaces;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UnarchiveSpaceCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        // WSP-022: a Space cannot become active again under an inactive parent.
        var parentIsActive = true;
        if (space.ParentSpaceId is { } parentId)
            parentIsActive = (await _spaces.FindParentFactAsync(parentId, ct))?.IsActive ?? false;

        space.Unarchive(_requestContext.UserId, _dateTimeProvider.UtcNow, parentIsActive);
        return Result.Success();
    }
}
