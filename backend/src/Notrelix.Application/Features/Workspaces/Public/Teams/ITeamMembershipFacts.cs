namespace Notrelix.Application.Features.Workspaces.Public.Teams;

/// <summary>
/// Workspaces-owned fact other contexts read instead of the team tables: Work Management asks it when a person
/// decides an approval step that names a team.
/// </summary>
public interface ITeamMembershipFacts
{
    /// <summary>
    /// Which of the given Teams the person belongs to right now: an active Team of this Workspace, an active
    /// Team member, and a still-active Workspace member. Anything else is left out, so the answer fails closed.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> CurrentTeamIdsAsync(
        Guid workspaceId, Guid userId, IReadOnlyCollection<Guid> teamIds, CancellationToken cancellationToken);

    /// <summary>
    /// The people who belong to the Team right now: active members of an active Team of this Workspace who are
    /// still active Workspace members. An unknown or inactive Team yields nobody, so the answer fails closed
    /// and never reaches outside the Workspace.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> CurrentMemberIdsAsync(
        Guid workspaceId, Guid teamId, CancellationToken cancellationToken);
}
