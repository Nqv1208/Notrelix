using System.Text.Json;

namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

/// <param name="Type">The block type (<c>paragraph</c>, <c>heading_1</c>, ... or the stored name such as <c>Heading1</c>).</param>
/// <param name="Content">Rich text v1 for a text-like type; omitted or <c>{}</c> starts the block empty.</param>
/// <param name="Properties">The type's own settings.</param>
/// <param name="Id">An id the client chooses; omitted lets the server choose.</param>
/// <param name="AfterBlockId">Sibling the block goes right after; omitted puts it last among its siblings.</param>
public record CreateBlockRequest(
    string Type,
    JsonElement? Content = null,
    JsonElement? Properties = null,
    Guid? Id = null,
    Guid? AfterBlockId = null,
    Guid? ParentId = null);
