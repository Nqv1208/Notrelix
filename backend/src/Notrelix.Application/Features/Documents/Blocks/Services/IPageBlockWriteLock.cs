namespace Notrelix.Application.Features.Documents.Blocks.Services;

/// <summary>
/// Serializes the writes that create, move or delete the blocks of one page. Sibling order and parent/child
/// links are decided from the page's block set, so two writers reading it at the same moment would place two
/// blocks on one key, or create a block under a parent that is deleted at that moment. Taking the lock first,
/// before the page's blocks are read, makes a second writer read what the first one committed. The lock is held
/// until the request's transaction ends and only ever waits for another writer of the same page.
/// </summary>
public interface IPageBlockWriteLock
{
    /// <summary>Must be called inside the request's data session, before the page's blocks are read.</summary>
    Task AcquireAsync(Guid pageId, CancellationToken cancellationToken);
}
