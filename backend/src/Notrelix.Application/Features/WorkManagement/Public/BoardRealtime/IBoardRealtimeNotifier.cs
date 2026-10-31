namespace Notrelix.Application.Features.WorkManagement.Public.BoardRealtime;

/// <summary>
/// Work Management-published way to tell the viewers of a Board that something on it changed. A handler
/// calls it once after its mutation succeeded: one notify-only <see cref="RealtimeResourceChangedV1"/>
/// is enrolled in the request's outbox transaction on the Board stream, never a payload. The viewers
/// refetch authoritative state over REST.
/// </summary>
public interface IBoardRealtimeNotifier
{
    /// <summary>
    /// Notifies the Board the request's resource lives in. A failed <see cref="Result"/> response and a
    /// resource that lives on no Board (a page comment) notify nothing. The change kind is the request
    /// type name without the <c>Command</c> suffix.
    /// </summary>
    Task NotifyAsync(object request, object? response, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies the Board a named resource lives in, for a request that is not scoped to the resource
    /// itself (a workspace-level decision about an approval request).
    /// </summary>
    Task NotifyResourceAsync(string resourceKind, Guid resourceId, string changeKind, object? response, CancellationToken cancellationToken);

    /// <summary>Notifies one named Board, for a change that touches a Board other than the resource's own.</summary>
    Task NotifyBoardAsync(Guid boardId, string changeKind, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies one named Board for a change made outside a request (an Automation rule acting as its
    /// executor), where the execution context carries no scope: the caller names the scope and the actor.
    /// </summary>
    Task NotifyBoardAsync(
        Guid boardId, string changeKind, Guid? accountId, Guid? workspaceId, Guid? actorUserId,
        Guid? correlationId, Guid? causationId, CancellationToken cancellationToken);
}
