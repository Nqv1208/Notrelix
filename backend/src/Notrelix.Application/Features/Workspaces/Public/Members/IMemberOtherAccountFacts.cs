namespace Notrelix.Application.Features.Workspaces.Public.Members;

/// <summary>
/// Producer-owned fact: is an active member of the request's workspace also an active member of a workspace of
/// another Account. A second factor belongs to the person across Accounts, so a reset may not reach such a
/// person. The caller must be an active Owner of the request's workspace; any other scope is refused.
/// </summary>
public interface IMemberOtherAccountFacts
{
    Task<bool> IsActiveInAnotherAccountAsync(Guid accountId, Guid workspaceId, Guid userId, CancellationToken cancellationToken);
}
