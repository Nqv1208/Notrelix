using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Commands.RenameTeam;

public record RenameTeamCommand(
    Guid WorkspaceId,
    Guid TeamId,
    string Name,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    // GOV-028: Team composition is managed by workspace Owners/Admins.
    public PermissionAction Action => PermissionAction.ManageTeams;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    // The concurrency authority is the Team aggregate; authorization stays on the Workspace (GOV-028).
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.team"), TeamId, WorkspaceId);
}

public class RenameTeamCommandHandler : IRequestHandler<RenameTeamCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RenameTeamCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RenameTeamCommand request, CancellationToken ct)
    {
        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.WorkspaceId == request.WorkspaceId, ct);

        if (team is null)
            throw new NotFoundException(nameof(Team), request.TeamId);

        // Team names are unique within the Workspace (Workspaces_Team_DuplicateName); the team itself is excluded.
        var otherTeamNames = await _context.Teams
            .AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId && t.DeletedAt == null && t.Id != request.TeamId)
            .Select(t => t.Name)
            .ToListAsync(ct);

        team.Rename(request.Name, _requestContext.UserId, _dateTimeProvider.UtcNow, otherTeamNames);
        return Result.Success();
    }
}
