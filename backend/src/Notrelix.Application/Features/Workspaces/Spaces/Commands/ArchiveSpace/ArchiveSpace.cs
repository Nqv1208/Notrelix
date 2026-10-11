using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;

public record ArchiveSpaceCommand(
    Guid WorkspaceId,
    Guid SpaceId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ArchiveSpaceCommandHandler : IRequestHandler<ArchiveSpaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceContentReader _content;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveSpaceCommandHandler(IWorkspaceDbContext context, ISpaceContentReader content, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _content = content;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveSpaceCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        // WSP-022: no cascade — active children or placed resources block the archive.
        var content = await _content.ReadAsync(request.WorkspaceId, space.Id, ct);
        space.Archive(_requestContext.UserId, _dateTimeProvider.UtcNow, content);
        return Result.Success();
    }
}
