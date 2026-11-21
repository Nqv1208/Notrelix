using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Commands.DeleteTeam;

public record DeleteTeamCommand(
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

/// <summary>The Space that still lists the Team, as the caller may see it.</summary>
public sealed record TeamReferenceDto(string Kind, Guid Id, string Name, string SpaceType);

/// <summary>
/// Deleting a Team that Spaces still list is refused (LD-16, stricter answer): the Owner or Admin removes the
/// entries first, so deleting never changes anyone's access behind their back. The refusal lists only the Spaces the
/// caller can see (GOV-029) and says "and others you cannot see" with no number when some are hidden, so no count
/// of hidden Spaces leaks. A Board grant of a Team is not listed yet: the Governance reads are keyed by Board, so
/// listing them would read every Board (named gap, a subject-keyed read, 0.2).
/// </summary>
public static class TeamReferences
{
    public const string StillReferencedCode = "workspaces.team.still-referenced";
}

public class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IAccessChangeSignal _access;
    private readonly IResourceVisibility _visibility;

    public DeleteTeamCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IAccessChangeSignal access, IResourceVisibility visibility)
    {
        _visibility = visibility;
        _access = access;
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

        // A repeat delete of a deleted Team is the domain no-op; only a live Team is checked for references.
        if (team.DeletedAt is null)
            await RefuseWhenReferencedAsync(request.WorkspaceId, request.TeamId, ct);

        team.Delete(_requestContext.UserId, _dateTimeProvider.UtcNow);
        _access.Workspace(request.WorkspaceId);
        return Result.Success();
    }

    private async Task RefuseWhenReferencedAsync(Guid workspaceId, Guid teamId, CancellationToken ct)
    {
        // Every non-deleted Space (active or archived) that lists the Team: it grants again when unarchived.
        var referencing = await _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && s.DeletedAt == null
                && s.Members.Any(m => m.SubjectType == SpaceMemberSubjectType.Team && m.SubjectId == teamId))
            .Select(s => new { s.Id, s.Name, s.SpaceType })
            .ToListAsync(ct);
        if (referencing.Count == 0) return;

        var visibleIds = (await _visibility.VisibleSpaceIdsAsync(workspaceId, _requestContext.UserId, ct)).ToHashSet();
        var visible = referencing
            .Where(s => visibleIds.Contains(s.Id))
            .OrderBy(s => s.Name)
            .Select(s => new TeamReferenceDto("space", s.Id, s.Name, s.SpaceType.ToString()))
            .ToList();

        throw new ConflictException("The Team is still used by Spaces. Remove its entries first, then delete it.", TeamReferences.StillReferencedCode)
        {
            Extensions = new Dictionary<string, object?>
            {
                ["references"] = visible,
                // True when some referencing Space is hidden from the caller: no number, so no count leaks.
                ["moreReferencesHidden"] = visible.Count < referencing.Count,
            },
        };
    }
}
