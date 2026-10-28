using System.Text;
using System.Text.Json;
using Notrelix.Application.Features.Documents.Blocks;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Application.Features.Documents.Versions.Services;

/// <summary>One block as a snapshot (or a template) holds it.</summary>
public sealed record SnapshotBlock(Guid Id, Guid? ParentId, BlockType Type, string Position, string Content, string Properties);

/// <summary>
/// The captured shape of a page's content: <c>{"schema":1,"title":...,"blocks":[...]}</c> with the blocks in
/// tree order (a parent before its children, siblings by position). The text is deterministic, so two captures
/// of the same content are byte-identical and a no-change capture is detectable by comparison.
/// </summary>
public static class PageVersionSnapshot
{
    public const int Schema = 1;

    public static string Build(string title, IEnumerable<Block> liveBlocks) => Write(title, InTreeOrder(liveBlocks));

    public static IReadOnlyList<SnapshotBlock> InTreeOrder(IEnumerable<Block> liveBlocks)
    {
        var blocks = liveBlocks.ToList();
        var ids = blocks.Select(block => block.Id).ToHashSet();
        var byParent = blocks
            .GroupBy(block => block.ParentId is { } parent && ids.Contains(parent) ? parent : (Guid?)null)
            .ToDictionary(group => group.Key ?? Guid.Empty, group => group.OrderBy(block => block.Position.Value, StringComparer.Ordinal).ToList());

        var ordered = new List<SnapshotBlock>(blocks.Count);
        var seen = new HashSet<Guid>();

        void Visit(Guid key)
        {
            if (!byParent.TryGetValue(key, out var children)) return;
            foreach (var block in children)
            {
                if (!seen.Add(block.Id)) continue;
                ordered.Add(new SnapshotBlock(
                    block.Id,
                    ids.Contains(block.ParentId ?? Guid.Empty) ? block.ParentId : null,
                    block.Type,
                    block.Position.Value,
                    block.Content?.Data.Value ?? "{}",
                    block.Properties?.Data.Value ?? "{}"));
                Visit(block.Id);
            }
        }

        Visit(Guid.Empty);
        return ordered;
    }

    /// <summary>Whether a history entry already holds exactly this captured text (compared in the stored, compact form).</summary>
    public static bool SameContent(DocumentVersion version, string capturedJson) =>
        string.Equals(version.Snapshot.Data?.Value, JsonValue.Create(capturedJson).Value, StringComparison.Ordinal);

    public static string Write(string title, IReadOnlyList<SnapshotBlock> blocks)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schema", Schema);
            writer.WriteString("title", title);
            writer.WriteStartArray("blocks");
            foreach (var block in blocks)
            {
                writer.WriteStartObject();
                writer.WriteString("id", block.Id);
                if (block.ParentId is { } parent) writer.WriteString("parentId", parent);
                else writer.WriteNull("parentId");
                writer.WriteString("type", block.Type.ToString());
                writer.WriteString("position", block.Position);
                writer.WritePropertyName("content");
                writer.WriteRawValue(block.Content, skipInputValidation: false);
                writer.WritePropertyName("properties");
                writer.WriteRawValue(block.Properties, skipInputValidation: false);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static (string Title, List<SnapshotBlock> Blocks) Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var title = root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() ?? "" : "";
        var blocks = new List<SnapshotBlock>();
        if (root.TryGetProperty("blocks", out var array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in array.EnumerateArray())
                blocks.Add(ParseBlock(item));
        }

        return (title, blocks);
    }

    /// <summary>The blocks of a template (<c>blocks_snapshot</c>) are the same array without the envelope.</summary>
    public static List<SnapshotBlock> ParseBlocks(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(ParseBlock).ToList()
            : [];
    }

    public static string WriteBlocks(IReadOnlyList<SnapshotBlock> blocks)
    {
        using var document = JsonDocument.Parse(Write("", blocks));
        return document.RootElement.GetProperty("blocks").GetRawText();
    }

    private static SnapshotBlock ParseBlock(JsonElement item)
    {
        var type = item.GetProperty("type").GetString()!;
        if (!BlockTypeNames.TryParse(type, out var blockType))
            throw new InvalidOperationException($"A captured block has the unknown type '{type}'.");

        Guid? parent = item.TryGetProperty("parentId", out var p) && p.ValueKind == JsonValueKind.String ? p.GetGuid() : null;
        return new SnapshotBlock(
            item.GetProperty("id").GetGuid(),
            parent,
            blockType,
            item.GetProperty("position").GetString()!,
            item.TryGetProperty("content", out var content) ? content.GetRawText() : "{}",
            item.TryGetProperty("properties", out var properties) ? properties.GetRawText() : "{}");
    }
}
