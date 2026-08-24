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
    public PermissionAction Action => PermissionAction.ManageWorkspace;
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
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.WorkspaceId == request.WorkspaceId, ct);

        if (team is null)
            throw new NotFoundException(nameof(Team), request.TeamId);

        var role = Enum.Parse<TeamMemberRole>(request.Role, ignoreCase: true);
        team.AddMember(request.UserId, role, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
