using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;

public record ArchiveSpaceCommand(
    Guid WorkspaceId,
    Guid SpaceId
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class ArchiveSpaceCommandHandler : IRequestHandler<ArchiveSpaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveSpaceCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveSpaceCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        space.Archive(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
