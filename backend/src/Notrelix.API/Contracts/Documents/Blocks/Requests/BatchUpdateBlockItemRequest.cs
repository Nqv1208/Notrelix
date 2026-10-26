using System.Text.Json;

namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

/// <param name="Op"><c>upsert</c> (default) creates the block with this id or edits the existing one; <c>delete</c> removes it and the blocks under it.</param>
/// <param name="Id">The block's id; the client chooses it for a create.</param>
/// <param name="Type">Required for a create; on an edit it retypes the block in place.</param>
/// <param name="Content">Rich text v1 for a text-like type; an empty object otherwise.</param>
/// <param name="Properties">The type's own settings.</param>
/// <param name="Position">An explicit fractional position key.</param>
/// <param name="AfterBlockId">The sibling to go right after, under <c>ParentId</c>.</param>
/// <param name="ParentId">The parent block; the page root when left out while a position or after-block is sent.</param>
/// <param name="ExpectedVersion">The block's version as read; required to edit or delete an existing block, ignored for a create.</param>
public record BatchUpdateBlockItemRequest(
    Guid Id,
    string? Op = null,
    string? Type = null,
    JsonElement? Content = null,
    JsonElement? Properties = null,
    string? Position = null,
    Guid? AfterBlockId = null,
    Guid? ParentId = null,
    long? ExpectedVersion = null);
