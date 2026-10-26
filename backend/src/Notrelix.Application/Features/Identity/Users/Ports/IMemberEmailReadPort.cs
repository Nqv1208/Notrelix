namespace Notrelix.Application.Features.Identity.Users.Ports;

/// <summary>
/// Identity-owned read of other people's sign-in email, scoped to one Workspace. The Workspaces disclosure
/// rule decides whether the reader may see an email (Owner or Admin of the Workspace, subjects active and
/// non-guest); an implementation that runs under a constrained database principal enforces the same rule
/// below the Application, so a mistake above it cannot widen what is returned.
/// </summary>
public interface IMemberEmailReadPort
{
    /// <summary>Lower-case emails of the given users in the Workspace; users the rule does not cover are omitted.</summary>
    Task<IReadOnlyDictionary<Guid, string>> FindAsync(
        Guid workspaceId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
