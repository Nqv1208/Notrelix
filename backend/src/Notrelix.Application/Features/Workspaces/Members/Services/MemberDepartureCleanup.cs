using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Services;

/// <summary>
/// A membership that ends (removal or leave) takes the person's User entries in Spaces and their Team
/// memberships with it, in the same unit of work (LD-12). The last Space Admin or Team Lead entry goes too: Owners
/// and Admins still manage every Space, and a Team left without a Lead is flagged "needs a lead" by readers.
/// Scoped to the one Workspace and its Account by explicit predicates on top of the tenant filter and the
/// database policies. Spaces and Teams that are soft-deleted are not read (the tenant filter and the soft-delete
/// filter are one), so a deleted one that is restored later still lists the person. Run it while the acting member still holds the Workspace grant, before the grant is revoked.
/// </summary>
internal static class MemberDepartureCleanup
{
    public static async Task RunAsync(
        IWorkspaceDbContext context,
        Workspace workspace,
        Guid userId,
        Guid actorId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var spaces = await context.Spaces
            .Include(s => s.Members)
            .Where(s => s.AccountId == workspace.AccountId
                && s.WorkspaceId == workspace.Id
                && s.Members.Any(m => m.SubjectType == SpaceMemberSubjectType.User && m.SubjectId == userId))
            .ToListAsync(ct);
        foreach (var space in spaces)
            space.RemoveDepartedUser(userId, actorId, now);

        var teams = await context.Teams
            .Include(t => t.Members)
            .Where(t => t.AccountId == workspace.AccountId
                && t.WorkspaceId == workspace.Id
                && t.Members.Any(m => m.UserId == userId && m.Status == TeamMemberStatus.Active))
            .ToListAsync(ct);
        foreach (var team in teams)
            team.RemoveDepartedMember(userId, actorId, now);
    }
}
