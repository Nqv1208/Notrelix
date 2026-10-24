using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembersExtra;

/// <param name="Admins">People whose highest role in the Space is Admin (Teams expanded, each person once).</param>
/// <param name="Invites">Pending, unexpired Workspace invitations that name the Space.</param>
public record SpaceMembersExtraSummaryDto(int Admins, int Members, int Viewers, int Invites);

/// <param name="Email">Shown only to a Workspace Owner or Admin and only for non-Guest members; null for every other reader.</param>
/// <param name="Title">The person's Workspace job title.</param>
/// <param name="LastActiveAt">Always null: no activity source exists yet.</param>
/// <param name="ActiveNow">Always false: no activity source exists yet.</param>
/// <param name="ViaTeams">Names of the Space's Team entries the person is also in.</param>
/// <param name="Creator">The person made the Space.</param>
public record SpaceMembersExtraPersonDto(
    Guid UserId,
    string? Email,
    string? Title,
    DateTimeOffset? LastActiveAt,
    bool ActiveNow,
    IReadOnlyList<string> ViaTeams,
    bool Creator);

/// <param name="Name">Always null: the invitee has no account name before accepting.</param>
/// <param name="Role">The Space role the invitation applies on acceptance: <c>Admin</c>, <c>Member</c> or <c>Viewer</c>.</param>
public record SpaceMembersExtraInvitationDto(Guid Id, string Email, string? Name, DateTimeOffset SentAt, string Role);

/// <param name="Level"><c>viewer</c>, <c>commenter</c>, <c>editor</c> or <c>manager</c>.</param>
public record SpaceMembersExtraGuestDto(
    Guid UserId,
    string Name,
    string? Email,
    bool Outside,
    Guid BoardId,
    string BoardName,
    string Level,
    DateTimeOffset? ExpiresAt);

/// <param name="Guests">Always empty for now: Board guests of a Space are read through the Board sharing contract (open decision).</param>
public record SpaceMembersExtraDto(
    SpaceMembersExtraSummaryDto Summary,
    IReadOnlyList<SpaceMembersExtraPersonDto> People,
    IReadOnlyList<SpaceMembersExtraInvitationDto> Invitations,
    IReadOnlyList<SpaceMembersExtraGuestDto> Guests);

/// <summary>
/// What the department "Members" tab adds to the Space member list (CTR-GAP-WS-MEMBERS-EXT): the people
/// reached by the Space's User and Team entries once each at their highest role, and the pending
/// invitations that name the Space (WSP-026). A Space the caller cannot see is NotFound (GOV-025,
/// GOV-029); a Guest does not enumerate the Workspace, so the read needs <c>ViewMembers</c> (GOV-009).
/// </summary>
public record GetSpaceMembersExtraQuery(Guid WorkspaceId, Guid SpaceId)
    : IQuery<Result<SpaceMembersExtraDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewMembers;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetSpaceMembersExtraQueryHandler : IRequestHandler<GetSpaceMembersExtraQuery, Result<SpaceMembersExtraDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceReachReader _reach;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;
    private readonly IMemberEmailDisclosure _emails;

    public GetSpaceMembersExtraQueryHandler(
        IWorkspaceDbContext context,
        ISpaceReachReader reach,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock,
        IMemberEmailDisclosure emails)
    {
        _emails = emails;
        _context = context;
        _reach = reach;
        _visibility = visibility;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<SpaceMembersExtraDto>> Handle(GetSpaceMembersExtraQuery request, CancellationToken ct)
    {
        var workspaceId = request.WorkspaceId;
        var visible = await _visibility.VisibleSpaceIdsAsync(workspaceId, _requestContext.UserId, ct);
        if (!visible.Contains(request.SpaceId))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var space = await _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && s.Id == request.SpaceId && s.DeletedAt == null)
            .Select(s => new { s.CreatedBy })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);

        var reach = (await _reach.ReadAsync(workspaceId, [request.SpaceId], ct))[request.SpaceId];
        var reachedPeople = reach.People;
        var emails = await _emails.ResolveAsync(workspaceId, reachedPeople.Select(p => p.UserId).ToList(), ct);
        var people = reachedPeople
            .Select(p => new SpaceMembersExtraPersonDto(
                p.UserId,
                emails.GetValueOrDefault(p.UserId),
                p.JobTitle,
                null,
                false,
                p.ViaTeams,
                space.CreatedBy == p.UserId))
            .ToList();

        var now = _clock.UtcNow;
        var pending = await _context.WorkspaceInvitations
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId
                && i.Status == WorkspaceInvitationStatus.Pending
                && i.ExpiresAt > now)
            .ToListAsync(ct);
        var invitations = pending
            .Where(i => !i.IsResourceInvitation)
            .Select(i => (Invitation: i, Entry: i.InitialEntries.FirstOrDefault(e =>
                e.Kind == WorkspaceInvitationEntryKind.Space && e.TargetId == request.SpaceId)))
            .Where(x => x.Entry is not null)
            .OrderBy(x => x.Invitation.CreatedAt)
            .Select(x => new SpaceMembersExtraInvitationDto(
                x.Invitation.Id,
                x.Invitation.Email,
                null,
                x.Invitation.CreatedAt,
                x.Entry!.Role))
            .ToList();

        var peopleRoles = reachedPeople.Select(p => p.Role).ToList();
        var summary = new SpaceMembersExtraSummaryDto(
            peopleRoles.Count(role => role is SpaceRole.Admin),
            peopleRoles.Count(role => role is SpaceRole.Member),
            peopleRoles.Count(role => role is SpaceRole.Viewer),
            invitations.Count);

        return Result<SpaceMembersExtraDto>.Success(new SpaceMembersExtraDto(summary, people, invitations, []));
    }
}
