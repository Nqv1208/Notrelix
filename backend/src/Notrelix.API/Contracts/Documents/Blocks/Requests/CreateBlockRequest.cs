namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

/// <param name="Properties">The block's JSON body (stored as the block content).</param>
/// <param name="AfterBlockId">Sibling the block goes right after; omitted puts it last among its siblings.</param>
public record CreateBlockRequest(string Type, string? Properties = null, Guid? AfterBlockId = null, Guid? ParentId = null);
