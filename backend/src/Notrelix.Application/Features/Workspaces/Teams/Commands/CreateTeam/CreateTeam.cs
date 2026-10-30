using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;

/// <summary>
/// Creates a Team. A retry under the same Idempotency-Key returns the Team the first attempt created; a different
/// body under that key is refused.
/// </summary>
[IdempotencyOperation("workspaces.team.create-team.v1")]
public record CreateTeamCommand(
    Guid WorkspaceId,
    string Name,
    string? Description
) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IIdempotentRequest
{
    // GOV-028: Team composition is managed by workspace Owners/Admins.
    public PermissionAction Action => PermissionAction.ManageTeams;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTeamCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateTeamCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var (team, _) = TeamFactory.CreateWithLead(
            workspace.AccountId,
            request.WorkspaceId,
            request.Name,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow);

        if (request.Description is not null)
            team.UpdateDescription(request.Description, _requestContext.UserId, _dateTimeProvider.UtcNow);

        _context.Teams.Add(team);
        return Result<Guid>.Success(team.Id);
    }
}
