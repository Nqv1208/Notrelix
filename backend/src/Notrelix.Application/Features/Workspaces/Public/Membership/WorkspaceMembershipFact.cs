namespace Notrelix.Application.Features.Workspaces.Public.Membership;

/// <summary>
/// Producer-owned workspace membership snapshot for one user in one workspace.
/// Workspaces owns membership lifecycle and status; this fact exposes exactly
/// the authorization-relevant projection a consumer needs — never Domain
/// aggregates or persistence types.
/// </summary>
public sealed record WorkspaceMembershipFact(
    Guid AccountId,
    Guid WorkspaceId,
    Guid UserId,
    bool IsActiveMember);

