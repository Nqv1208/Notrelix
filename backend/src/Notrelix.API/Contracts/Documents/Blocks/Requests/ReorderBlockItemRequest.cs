namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

/// <param name="BlockId">The block to move.</param>
/// <param name="ExpectedVersion">The block's version as read; a stale value answers 412 with the current block.</param>
/// <param name="ParentId">The new parent block; the page root when left out.</param>
/// <param name="AfterBlockId">The sibling to go right after, under <c>ParentId</c>.</param>
/// <param name="BeforeBlockId">The sibling to go right before (to move to the top); neither named puts the block last.</param>
/// <param name="Position">A client sort key, kept only when it lies strictly between the siblings named.</param>
public record ReorderBlockItemRequest(
    Guid BlockId,
    long ExpectedVersion,
    Guid? ParentId = null,
    Guid? AfterBlockId = null,
    Guid? BeforeBlockId = null,
    string? Position = null);
