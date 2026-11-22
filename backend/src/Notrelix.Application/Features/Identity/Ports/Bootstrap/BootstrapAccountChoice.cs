namespace Notrelix.Application.Features.Identity.Ports.Bootstrap;

/// <summary>
/// An active Account membership of the member, with the Account's name for ordering. Whether the member owns
/// the Account is a plain fact here: Identity does not name another context's role type.
/// </summary>
public sealed record BootstrapAccountMembership(Guid AccountId, bool IsOwner, string AccountName);

/// <summary>
/// Chooses the Account a member acts in when the request names none. The choice depends only on the
/// member's facts, never on the order the database returned them in, so the same member gets the same
/// Account on every call:
/// 1. the Account of the member's default Workspace;
/// 2. else an Account the member owns, then any other, in the order Owner role first, Account name
///    (ignoring case, no culture), Account id;
/// 3. else the Account of a Workspace the member belongs to, lowest Account id first.
/// </summary>
public static class BootstrapAccountChoice
{
    public static Guid? Choose(
        Guid? defaultWorkspaceId,
        IReadOnlyCollection<BootstrapWorkspaceProjection> workspaces,
        IReadOnlyCollection<BootstrapAccountMembership> memberships)
    {
        if (defaultWorkspaceId is { } defaultId
            && workspaces.FirstOrDefault(w => w.Id == defaultId) is { } defaultWorkspace)
        {
            return defaultWorkspace.AccountId;
        }

        return Own(memberships)
            ?? workspaces.Select(w => w.AccountId).Order().Select(id => (Guid?)id).FirstOrDefault();
    }

    /// <summary>The member's own Account: an owned one first, else any membership, in the stable order above.</summary>
    public static Guid? Own(IReadOnlyCollection<BootstrapAccountMembership> memberships) =>
        memberships
            .OrderBy(m => m.IsOwner ? 0 : 1)
            .ThenBy(m => m.AccountName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.AccountId)
            .Select(m => (Guid?)m.AccountId)
            .FirstOrDefault();
}
