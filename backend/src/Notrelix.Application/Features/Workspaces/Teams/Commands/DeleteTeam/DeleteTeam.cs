using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Commands.DeleteTeam;

public record DeleteTeamCommand(
    Guid WorkspaceId,
    Guid TeamId
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteTeamCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteTeamCommand request, CancellationToken ct)
    {
        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.WorkspaceId == request.WorkspaceId, ct);

        if (team is null)
            throw new NotFoundException(nameof(Team), request.TeamId);

        team.Delete(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
