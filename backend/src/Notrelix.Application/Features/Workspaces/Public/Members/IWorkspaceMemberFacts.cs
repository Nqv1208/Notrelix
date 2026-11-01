namespace Notrelix.Application.Features.Workspaces.Public.Members;

/// <summary>A person's standing in one workspace: the built-in role Workspaces stores and whether it is active.</summary>
public sealed record WorkspaceMemberFact(Guid UserId, string Role, bool IsActive);

/// <summary>
/// Workspaces-owned fact other contexts read instead of the membership table: Governance asks it before it
/// attaches a custom role to a person.
/// </summary>
public interface IWorkspaceMemberFacts
{
    Task<WorkspaceMemberFact?> FindAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);
}
