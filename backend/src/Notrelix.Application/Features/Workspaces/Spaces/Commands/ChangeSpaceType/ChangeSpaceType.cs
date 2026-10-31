using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceType;

public record ChangeSpaceTypeCommand(
    Guid WorkspaceId,
    Guid SpaceId,
    string SpaceType
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    // GOV-028: ManageSpaces on the Space itself (workspace Owner/Admin, or an
    // effective Space Admin of the Space or one of its ancestors).
    public PermissionAction Action => PermissionAction.ManageSpaces;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.space"), SpaceId, WorkspaceId);
}

public class ChangeSpaceTypeCommandHandler : IRequestHandler<ChangeSpaceTypeCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IAccessChangeSignal _access;

    public ChangeSpaceTypeCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IAccessChangeSignal access)
    {
        _access = access;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ChangeSpaceTypeCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var spaceType = Enum.Parse<SpaceType>(request.SpaceType, ignoreCase: true);
        space.ChangeType(spaceType, _requestContext.UserId, _dateTimeProvider.UtcNow);
        _access.Workspace(request.WorkspaceId);
        return Result.Success();
    }
}
