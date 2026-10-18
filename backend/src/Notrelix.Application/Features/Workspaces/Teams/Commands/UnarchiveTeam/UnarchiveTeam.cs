using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Commands.UnarchiveTeam;

public record UnarchiveTeamCommand(
    Guid WorkspaceId,
    Guid TeamId,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    // GOV-028: Team composition is managed by workspace Owners/Admins.
    public PermissionAction Action => PermissionAction.ManageTeams;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    // The concurrency authority is the Team aggregate; authorization stays on the Workspace (GOV-028).
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.team"), TeamId, WorkspaceId);
}

public class UnarchiveTeamCommandHandler : IRequestHandler<UnarchiveTeamCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UnarchiveTeamCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UnarchiveTeamCommand request, CancellationToken ct)
    {
        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.WorkspaceId == request.WorkspaceId, ct);

        if (team is null)
            throw new NotFoundException(nameof(Team), request.TeamId);

        team.Unarchive(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
