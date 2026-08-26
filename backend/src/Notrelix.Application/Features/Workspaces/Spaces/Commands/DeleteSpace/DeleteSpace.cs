using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.DeleteSpace;

public record DeleteSpaceCommand(
    Guid WorkspaceId,
    Guid SpaceId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class DeleteSpaceCommandHandler : IRequestHandler<DeleteSpaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteSpaceCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        space.Delete(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
