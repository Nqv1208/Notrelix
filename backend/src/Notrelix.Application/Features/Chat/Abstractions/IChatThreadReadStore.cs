namespace Notrelix.Application.Features.Chat.Abstractions;

/// <summary>
/// Moves a person's read pointer in a thread forward (UC-CHT-F08). One UPDATE sets the pointer to GREATEST(current, target)
/// on the caller's own follow row, so a pointer never moves backwards, even when two requests race. A person who does not
/// follow the thread has no row and nothing moves.
/// </summary>
public interface IChatThreadReadStore
{
    /// <summary>Moves the pointer to the target when it is ahead. Returns false when the caller does not follow the thread.</summary>
    Task<bool> AdvanceAsync(Guid workspaceId, Guid rootMessageId, Guid userId, long target, CancellationToken cancellationToken);
}
