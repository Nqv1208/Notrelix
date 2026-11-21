using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>
/// Workspaces-owned handling of the initial Team/Space entries a Workspace
/// invitation names (WSP-026): eligibility at invite time, and atomic
/// application at accept time. Entries are an invitation fact; they grant
/// nothing until the invitation is accepted (WSP-006).
/// </summary>
public sealed class InvitationInitialEntries
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceStore _spaces;

    public InvitationInitialEntries(IWorkspaceDbContext context, ISpaceStore spaces)
    {
        _context = context;
        _spaces = spaces;
    }

    /// <summary>
    /// The entries whose Team is not an active Team, or whose Space is not an active Space, of the Workspace, each
    /// with its position in <paramref name="entries"/>. Empty when every target is eligible. Pure read, no mutation.
    /// </summary>
    /// <param name="inviterVisibleSpaceIds">
    /// The Spaces the inviter may see (Governance visibility). A Space outside it is refused with the same reason as a
    /// missing one, so an inviter learns nothing about a Space they cannot see. Null at acceptance: the invitation was
    /// checked against its issuer when it was issued, and the invitee's own visibility is not the question there.
    /// </param>
    public async Task<IReadOnlyList<InvitationEntryRefusal>> IneligibleEntriesAsync(
        Guid workspaceId,
        IReadOnlyList<WorkspaceInvitationInitialEntry> entries,
        IReadOnlySet<Guid>? inviterVisibleSpaceIds,
        CancellationToken ct)
    {
        var refused = new List<InvitationEntryRefusal>();
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var eligible = entry.Kind == WorkspaceInvitationEntryKind.Team
                ? await _spaces.IsActiveTeamAsync(workspaceId, entry.TargetId, ct)
                : (inviterVisibleSpaceIds is null || inviterVisibleSpaceIds.Contains(entry.TargetId))
                  && await IsActiveSpaceAsync(workspaceId, entry.TargetId, ct);
            if (!eligible)
                refused.Add(new InvitationEntryRefusal(
                    index, entry.Kind.ToString().ToLowerInvariant(), entry.TargetId, InvitationEntryRefusal.NotActive));
        }

        return refused;
    }

    /// <summary>True when every named Team is an active Team and every named Space an active Space of the Workspace.</summary>
    public async Task<bool> AllTargetsEligibleAsync(
        Guid workspaceId,
        IReadOnlyList<WorkspaceInvitationInitialEntry> entries,
        CancellationToken ct) =>
        (await IneligibleEntriesAsync(workspaceId, entries, null, ct)).Count == 0;

    /// <summary>
    /// Applies every entry for the (already active) workspace member. Subjects
    /// already holding an entry are left unchanged (never re-roled by an
    /// invitation). Runs in the accept transaction: any failure rolls the
    /// membership back with it. Callers must have verified
    /// <see cref="AllTargetsEligibleAsync"/> first.
    /// </summary>
    public async Task ApplyAsync(
        Guid workspaceId,
        Guid userId,
        Guid workspaceMemberId,
        Guid addedBy,
        IReadOnlyList<WorkspaceInvitationInitialEntry> entries,
        DateTimeOffset now,
        CancellationToken ct)
    {
        foreach (var entry in entries.Where(e => e.Kind == WorkspaceInvitationEntryKind.Team))
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == entry.TargetId && t.WorkspaceId == workspaceId && t.DeletedAt == null, ct)
                ?? throw new NotFoundException(nameof(Team), entry.TargetId);

            team.AddMember(userId, Enum.Parse<TeamMemberRole>(entry.Role), addedBy, now, workspaceMemberId);
        }

        foreach (var entry in entries.Where(e => e.Kind == WorkspaceInvitationEntryKind.Space))
        {
            var space = await _spaces.FindAsync(workspaceId, entry.TargetId, ct)
                ?? throw new NotFoundException(nameof(Space), entry.TargetId);

            if (space.Members.Any(m => m.SubjectType == SpaceMemberSubjectType.User && m.SubjectId == userId))
                continue;

            space.AddMember(SpaceMemberSubjectType.User, userId, Enum.Parse<SpaceRole>(entry.Role), addedBy, now);
        }
    }

    private async Task<bool> IsActiveSpaceAsync(Guid workspaceId, Guid spaceId, CancellationToken ct) =>
        await _context.Spaces.AsNoTracking().AnyAsync(
            s => s.Id == spaceId && s.WorkspaceId == workspaceId && s.DeletedAt == null && s.Status == SpaceStatus.Active, ct);
}
