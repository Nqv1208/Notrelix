using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.DeleteSpace;

public record DeleteSpaceCommand(
    Guid WorkspaceId,
    Guid SpaceId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    // GOV-028: ManageSpaces on the Space itself (workspace Owner/Admin, or an
    // effective Space Admin of the Space or one of its ancestors).
    public PermissionAction Action => PermissionAction.ManageSpaces;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.space"), SpaceId, WorkspaceId);
}

public class DeleteSpaceCommandHandler : IRequestHandler<DeleteSpaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceContentReader _content;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteSpaceCommandHandler(IWorkspaceDbContext context, ISpaceContentReader content, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _content = content;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        // WSP-022: no cascade — child Spaces or placed resources block the delete.
        var content = await _content.ReadAsync(request.WorkspaceId, space.Id, ct);
        space.Delete(_requestContext.UserId, _dateTimeProvider.UtcNow, content);
        return Result.Success();
    }
}
