using System.Text.Json;

namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

/// <param name="Type">Retypes the block in place (it keeps its id, position and parent).</param>
/// <param name="Content">Rich text v1 for a text-like type; an empty object otherwise.</param>
/// <param name="Properties">The type's own settings, replacing the current ones.</param>
/// <param name="ExpectedVersion">The block's version as read; a stale value answers 412 with the current block.</param>
public record UpdateBlockRequest(
    string? Type = null,
    JsonElement? Content = null,
    JsonElement? Properties = null,
    long? ExpectedVersion = null);
