using Notrelix.Application.Features.Workspaces.Public.Teams;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Services;

/// <summary>The step a person may decide on a request, and the Teams that right came from.</summary>
public sealed record DeciderStep(ApprovalStep? Step, IReadOnlyCollection<Guid> TeamIds);

public static class ApprovalDeciders
{
    /// <summary>
    /// The step <paramref name="userId"/> may decide: one that names the person, or one that names a Team the person
    /// belongs to at this moment (read through Workspaces, never cached). A pending step is preferred over a decided one.
    /// </summary>
    public static async Task<DeciderStep> FindAsync(
        ApprovalRequest approval, Guid workspaceId, Guid userId, ITeamMembershipFacts teams, CancellationToken ct)
    {
        var named = approval.Steps.Where(s => s.ApproverTeamId.HasValue).Select(s => s.ApproverTeamId!.Value).Distinct().ToList();
        var mine = named.Count == 0 ? [] : await teams.CurrentTeamIdsAsync(workspaceId, userId, named, ct);
        var step = approval.Steps
            .Where(s => s.ApproverUserId == userId || (s.ApproverTeamId is { } t && mine.Contains(t)))
            .OrderBy(s => s.Status == ApprovalStatus.Pending ? 0 : 1)
            .ThenBy(s => s.Position)
            .FirstOrDefault();
        return new DeciderStep(step, mine);
    }
}
