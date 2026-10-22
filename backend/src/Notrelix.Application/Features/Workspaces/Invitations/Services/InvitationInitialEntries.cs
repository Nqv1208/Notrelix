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
    /// True when every named Team is an active Team and every named Space an
    /// active Space of the Workspace. Pure read, no mutation.
    /// </summary>
    public async Task<bool> AllTargetsEligibleAsync(
        Guid workspaceId,
        IReadOnlyList<WorkspaceInvitationInitialEntry> entries,
        CancellationToken ct)
    {
        foreach (var entry in entries)
        {
            var eligible = entry.Kind == WorkspaceInvitationEntryKind.Team
                ? await _spaces.IsActiveTeamAsync(workspaceId, entry.TargetId, ct)
                : await IsActiveSpaceAsync(workspaceId, entry.TargetId, ct);
            if (!eligible) return false;
        }

        return true;
    }

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
