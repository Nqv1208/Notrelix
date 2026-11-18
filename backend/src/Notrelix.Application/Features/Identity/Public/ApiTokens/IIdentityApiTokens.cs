namespace Notrelix.Application.Features.Identity.Public.ApiTokens;

/// <summary>Why a person's access to a Workspace ended, as Workspaces tells Identity.</summary>
public enum MemberAccessEnd
{
    Removed,
    Suspended,
    Left
}

/// <summary>
/// Producer-owned public surface for ending a person's API tokens when their access to a Workspace ends.
/// Whether the person still belongs to the Workspace is a Workspaces fact: Workspaces calls this in the same
/// transaction as the membership change, so a token never outlives the membership it was issued under.
/// </summary>
public interface IIdentityApiTokens
{
    /// <summary>
    /// Revokes the person's active tokens in <paramref name="workspaceId"/>, recording who acted and why.
    /// Tokens of other people, of other Workspaces and tokens already revoked are left as they are, so a retry
    /// revokes nothing more. Returns how many tokens stopped.
    /// </summary>
    Task<int> RevokeForMemberAsync(
        Guid userId,
        Guid workspaceId,
        Guid revokedBy,
        MemberAccessEnd reason,
        CancellationToken cancellationToken);
}
