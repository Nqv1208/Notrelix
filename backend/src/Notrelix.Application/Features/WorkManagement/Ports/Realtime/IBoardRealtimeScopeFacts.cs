namespace Notrelix.Application.Features.WorkManagement.Ports.Realtime;

/// <summary>
/// Work Management port: the Board a resource that changed lives on, so its viewers can be notified.
/// Covers the Board itself, every Board child, and the resources that point at a Board item (approval
/// requests, comments, attachments). Deleted resources resolve too. Null when the resource lives on no
/// Board (a page comment) or no longer exists.
/// </summary>
public interface IBoardRealtimeScopeFacts
{
    Task<Guid?> ResolveBoardIdAsync(string resourceKind, Guid resourceId, CancellationToken cancellationToken);

    /// <summary>
    /// The request's data session holds a change that the commit will write. A semantic no-op holds none and
    /// announces nothing, as it bumps nothing.
    /// </summary>
    bool HasPendingChanges();
}
