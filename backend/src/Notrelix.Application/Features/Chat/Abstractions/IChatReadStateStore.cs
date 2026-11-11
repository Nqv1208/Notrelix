namespace Notrelix.Application.Features.Chat.Abstractions;

/// <summary>
/// Moves the caller's read boundaries in the database (CHT-009). Each call is one UPDATE that takes the membership row
/// lock and sets the boundary to GREATEST(current, target), so a boundary never moves backwards, even when two requests
/// race. Only the caller's own active memberships are touched.
/// </summary>
public interface IChatReadStateStore
{
    /// <summary>Moves the boundary in one space to the target when it is ahead. Returns false when the caller has no active membership there.</summary>
    Task<bool> AdvanceAsync(Guid workspaceId, Guid spaceId, Guid userId, long target, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the boundary in every live space of the Workspace to that space's last sequence (UC-CHT-D07). The statement sees
    /// the sequences committed when it starts, so a message sent during the call stays unread. Returns the rows it touched.
    /// </summary>
    Task<int> AdvanceAllAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);
}
