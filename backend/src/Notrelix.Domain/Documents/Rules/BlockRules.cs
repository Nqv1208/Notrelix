using System.Text.Json;
using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.Domain.Documents.Rules;

public static class BlockContentValidator
{
    public static void Validate(BlockType type, BlockContent content)
    {
        if (content.Data.Value == null)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Block_ContentCannotBeNull, "Block content data cannot be null.");

        if (type == BlockType.PageReference)
            ValidatePageReference(content);
    }

    /// <summary>
    /// A page reference may start empty (right after a type change, before the
    /// author picks the page); once it names a target it must be a page id.
    /// </summary>
    private static void ValidatePageReference(BlockContent content)
    {
        using var document = JsonDocument.Parse(content.Data.Value);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return;
        if (!document.RootElement.TryGetProperty(BlockContentConversion.LinkedPageIdKey, out var target)) return;

        var valid = target.ValueKind == JsonValueKind.String
            && Guid.TryParse(target.GetString(), out var id)
            && id != Guid.Empty;
        if (!valid)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Block_PageReferenceTargetInvalid, "A page reference must name a page by its id.");
    }
}

/// <summary>
/// Content conversion for a block type change (the block keeps its id): text
/// carries over between text-like types; any other change starts empty.
/// </summary>
public static class BlockContentConversion
{
    public const string LinkedPageIdKey = "linkedPageId";

    private static readonly HashSet<BlockType> TextLike =
    [
        BlockType.Text, BlockType.Heading1, BlockType.Heading2, BlockType.Heading3,
        BlockType.BulletList, BlockType.NumberedList, BlockType.ToDo, BlockType.Toggle,
        BlockType.Quote, BlockType.Callout, BlockType.Code
    ];

    public static bool IsTextLike(BlockType type) => TextLike.Contains(type);

    public static BlockContent Convert(BlockType from, BlockType to, BlockContent content)
    {
        if (!IsTextLike(from) || !IsTextLike(to)) return BlockContent.Create(JsonValue.EmptyObject());

        using var document = JsonDocument.Parse(content.Data.Value);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return BlockContent.Create(JsonValue.EmptyObject());

        var converted = new Dictionary<string, JsonElement>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            // Shape-specific members do not survive into a type that lacks them.
            if (property.NameEquals("checked") && to != BlockType.ToDo) continue;
            if (property.NameEquals("language") && to != BlockType.Code) continue;
            converted[property.Name] = property.Value.Clone();
        }

        var json = JsonSerializer.Serialize(converted);
        return BlockContent.Create(JsonValue.Create(json));
    }
}
