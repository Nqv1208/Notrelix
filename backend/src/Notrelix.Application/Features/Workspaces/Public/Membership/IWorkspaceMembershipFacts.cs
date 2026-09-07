namespace Notrelix.Application.Features.Workspaces.Public.Membership;

/// <summary>
/// Producer-owned read source for workspace membership facts. Consumers compose
/// this published fact into their own authorization evaluation; they never
/// query Workspace persistence directly.
/// </summary>
public interface IWorkspaceMembershipFacts
{
    Task<WorkspaceMembershipFact?> ResolveAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken);
}