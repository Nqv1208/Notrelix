namespace Notrelix.Application.Features.Documents.Public.PageBlocks;

/// <summary>
/// Documents-published fact: whether a live Block belongs to a live Page of a Workspace. Collaboration uses it to
/// accept a comment anchor that names a block of the page; it never reads Documents persistence itself, and the
/// answer says nothing about who may see the page (the caller authorized the page first).
/// </summary>
public interface IPageBlockFacts
{
    Task<bool> BlockBelongsToPageAsync(Guid workspaceId, Guid pageId, Guid blockId, CancellationToken cancellationToken);
}
