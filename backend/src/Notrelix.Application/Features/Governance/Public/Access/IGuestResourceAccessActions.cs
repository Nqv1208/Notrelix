namespace Notrelix.Application.Features.Governance.Public.Access;

/// <summary>
/// Producer-owned public target action: Governance creates the explicit
/// resource entry that a resource invitation (WSP-026) carries, when the
/// invitation is accepted. Governance decides and persists the entry; the
/// caller (Workspaces, AcceptInvitation) only requests it.
/// </summary>
/// <remarks>
/// The entry's subject is always the guest's <c>User</c> identity — an
/// <c>ExternalEmail</c> subject is never created by this flow (WSP-026). The
/// level is capped at Editor (GOV-030): Manager or higher is refused. The action
/// runs inside the caller's request transaction so the Workspace membership and
/// the entry become visible atomically.
/// An existing entry of the same subject on the same resource is reused: it is
/// raised to the invited level when lower and never lowered.
/// </remarks>
public interface IGuestResourceAccessActions
{
    Task<GuestResourceEntryOutcome> EnsureGuestEntryAsync(
        GuestResourceEntryRequest request,
        CancellationToken cancellationToken);
}

/// <param name="ResourceKind">Canonical kind: <c>work-management.board</c> or <c>documents.page</c>.</param>
/// <param name="Level"><c>Viewer</c>, <c>Commenter</c> or <c>Editor</c>.</param>
public sealed record GuestResourceEntryRequest(
    Guid AccountId,
    Guid WorkspaceId,
    string ResourceKind,
    Guid ResourceId,
    Guid GuestUserId,
    string Level,
    Guid GrantedBy,
    DateTimeOffset Now);

public enum GuestResourceEntryOutcome
{
    Created,
    Raised,
    Unchanged,
    /// <summary>The resource is missing, deleted, archived or in another Workspace.</summary>
    ResourceUnavailable
}
