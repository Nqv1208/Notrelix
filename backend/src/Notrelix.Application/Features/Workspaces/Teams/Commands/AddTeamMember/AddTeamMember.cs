using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;

public record AddTeamMemberCommand(
    Guid WorkspaceId,
    Guid TeamId,
    Guid UserId,
    string Role
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    // GOV-028: Team composition is managed by workspace Owners/Admins.
    public PermissionAction Action => PermissionAction.ManageTeams;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class AddTeamMemberCommandHandler : IRequestHandler<AddTeamMemberCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddTeamMemberCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AddTeamMemberCommand request, CancellationToken ct)
    {
        var team = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.WorkspaceId == request.WorkspaceId, ct);

        if (team is null)
            throw new NotFoundException(nameof(Team), request.TeamId);

        // WSP-014: a Team member is an active member of the same Workspace. Team
        // grants are evaluated by Governance (GOV-026), so a raw user id from
        // outside the Workspace must never become a Team member.
        var workspaceMember = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId && m.Status == WorkspaceMemberStatus.Active)
            .Select(m => new { m.Id })
            .FirstOrDefaultAsync(ct);
        if (workspaceMember is null)
            throw new NotFoundException(nameof(WorkspaceMember), request.UserId);

        var role = Enum.Parse<TeamMemberRole>(request.Role, ignoreCase: true);
        team.AddMember(request.UserId, role, _requestContext.UserId, _dateTimeProvider.UtcNow, workspaceMember.Id);
        return Result.Success();
    }
}
