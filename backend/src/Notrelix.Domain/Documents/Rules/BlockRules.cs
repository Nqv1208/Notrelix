using System.Buffers;
using System.Text.Json;
using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.Domain.Documents.Rules;

/// <summary>
/// Block content schema per Block Type (DCT-007/008). A text-like Block stores rich text v1
/// (<c>{"v":1,"runs":[...]}</c>); every other type keeps its meaning in properties and has an empty content
/// object. <see cref="Normalize"/> is the only way content reaches a Block, so what is stored is always the
/// canonical form.
/// </summary>
public static class BlockContentValidator
{
    public static void Validate(BlockType type, BlockContent content) => Normalize(type, content);

    /// <summary>
    /// True when the stored content already is the canonical <paramref name="candidate"/>. Stored JSON may
    /// list its members in another order (jsonb does), so the stored side is normalised before comparing;
    /// content that is not valid for the type (not yet upgraded) is never the same.
    /// </summary>
    public static bool AreSame(BlockType type, BlockContent stored, BlockContent candidate)
    {
        if (stored == candidate) return true;
        try
        {
            return Normalize(type, stored) == candidate;
        }
        catch (BusinessRuleException)
        {
            return false;
        }
    }

    public static BlockContent Normalize(BlockType type, BlockContent content)
    {
        if (content.Data.Value == null)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Block_ContentCannotBeNull, "Block content data cannot be null.");

        using var document = JsonDocument.Parse(content.Data.Value);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Null)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Block_ContentCannotBeNull, "Block content data cannot be null.");
        if (root.ValueKind != JsonValueKind.Object)
            throw Invalid("Block content must be an object.");

        if (!BlockContentConversion.IsTextLike(type))
        {
            if (root.EnumerateObject().Any())
                throw Invalid($"A {type} block keeps its data in properties; its content must be empty.");
            return BlockContent.Create(JsonValue.EmptyObject());
        }

        var runs = ParseTextContent(type, root);
        return BlockContent.Create(JsonValue.Create(RichText.ToJson(runs)));
    }

    private static IReadOnlyList<RichTextRun> ParseTextContent(BlockType type, JsonElement root)
    {
        // An empty object is an empty block (what a block starts as, or becomes after a type change).
        if (!root.EnumerateObject().Any()) return [];

        var hasVersion = false;
        JsonElement runs = default;
        var hasRuns = false;
        foreach (var member in root.EnumerateObject())
        {
            switch (member.Name)
            {
                case "v":
                    if (member.Value.ValueKind != JsonValueKind.Number || !member.Value.TryGetInt32(out var version) || version != RichText.Version)
                        throw Invalid("Block content version must be 1.");
                    hasVersion = true;
                    break;
                case "runs":
                    runs = member.Value;
                    hasRuns = true;
                    break;
                default:
                    throw Invalid($"Block content has an unknown member '{member.Name}'.");
            }
        }

        if (!hasVersion || !hasRuns)
            throw Invalid("Rich text content needs both 'v' (1) and 'runs'.");

        return RichText.ParseRuns(runs, allowMarks: type != BlockType.Code, "runs");
    }

    private static BusinessRuleException Invalid(string message) =>
        new(DocumentRuleCodes.Documents_Block_ContentInvalid, message);
}

/// <summary>
/// Block properties schema per Block Type: closed sets, no unknown members. Everything is optional so a
/// block can start empty (for example an image before its upload finishes); what is present must be valid.
/// </summary>
public static class BlockPropertiesValidator
{
    private static readonly HashSet<string> Align = ["left", "center", "right", "justify"];
    private static readonly HashSet<string> LineSpacings = ["tight", "normal", "relaxed", "loose"];
    private static readonly HashSet<string> Tones = ["info", "success", "warning", "danger", "neutral"];

    private const int MaxIndent = 8;
    private const int MaxTableColumns = 50;
    private const int MaxTableRows = 500;
    private const int MaxTableTextLength = 200_000;

    public static IReadOnlySet<string> AllowedKeys(BlockType type)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (BlockContentConversion.IsTextLike(type)) keys.UnionWith(["align", "indent", "lineSpacing"]);
        switch (type)
        {
            case BlockType.ToDo: keys.Add("checked"); break;
            case BlockType.Code: keys.Add("language"); break;
            case BlockType.Callout: keys.Add("icon"); keys.Add("tone"); break;
            case BlockType.Toggle: keys.Add("collapsed"); break;
            case BlockType.Image: keys.UnionWith(["attachmentId", "width", "caption", "alt"]); break;
            case BlockType.File: keys.Add("attachmentId"); break;
            case BlockType.Video: keys.Add("attachmentId"); break;
            case BlockType.Board: keys.UnionWith(["boardId", "viewId"]); break;
            case BlockType.PageReference: keys.Add("pageId"); break;
            case BlockType.Table: keys.UnionWith(["rows", "headerRow", "colWidths"]); break;
        }

        return keys;
    }

    /// <summary>The properties a Block keeps when its type changes: members the new type allows.</summary>
    public static BlockProperties Convert(BlockType to, BlockProperties properties)
    {
        using var document = JsonDocument.Parse(properties.Data.Value);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return BlockProperties.Empty();

        var allowed = AllowedKeys(to);
        var kept = document.RootElement.EnumerateObject().Where(p => allowed.Contains(p.Name)).ToList();
        if (kept.Count == 0) return BlockProperties.Empty();

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, RichText.WriterOptions))
        {
            writer.WriteStartObject();
            foreach (var property in kept)
            {
                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return BlockProperties.Create(JsonValue.Create(System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan)));
    }

    /// <summary>See <see cref="BlockContentValidator.AreSame"/>: the stored side is normalised before comparing.</summary>
    public static bool AreSame(BlockType type, BlockProperties stored, BlockProperties candidate)
    {
        if (stored == candidate) return true;
        try
        {
            return Normalize(type, stored) == candidate;
        }
        catch (BusinessRuleException)
        {
            return false;
        }
    }

    public static BlockProperties Normalize(BlockType type, BlockProperties properties)
    {
        using var document = JsonDocument.Parse(properties.Data.Value);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw Invalid("Block properties must be an object.");

        var allowed = AllowedKeys(type);
        foreach (var member in root.EnumerateObject())
        {
            if (!allowed.Contains(member.Name))
                throw Invalid($"A {type} block has no property '{member.Name}'.");
        }

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, RichText.WriterOptions))
        {
            writer.WriteStartObject();
            foreach (var key in allowed.Order(StringComparer.Ordinal))
            {
                if (!root.TryGetProperty(key, out var value)) continue;
                writer.WritePropertyName(key);
                WriteProperty(writer, key, value);
            }

            writer.WriteEndObject();
        }

        return BlockProperties.Create(JsonValue.Create(System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan)));
    }

    private static void WriteProperty(Utf8JsonWriter writer, string key, JsonElement value)
    {
        switch (key)
        {
            case "align":
                writer.WriteStringValue(OneOf(key, value, Align));
                break;
            case "tone":
                writer.WriteStringValue(OneOf(key, value, Tones));
                break;
            case "lineSpacing":
                writer.WriteStringValue(OneOf(key, value, LineSpacings));
                break;
            case "indent":
                writer.WriteNumberValue(Int(key, value, 0, MaxIndent));
                break;
            case "checked":
            case "collapsed":
            case "headerRow":
                writer.WriteBooleanValue(Bool(key, value));
                break;
            case "language":
                writer.WriteStringValue(Language(value));
                break;
            case "icon":
                writer.WriteStringValue(Text(key, value, 16, required: true));
                break;
            case "alt":
                writer.WriteStringValue(Text(key, value, 500, required: false));
                break;
            case "attachmentId":
            case "boardId":
            case "viewId":
                writer.WriteStringValue(Id(key, value, DocumentRuleCodes.Documents_Block_PropertiesInvalid));
                break;
            case "pageId":
                writer.WriteStringValue(Id(key, value, DocumentRuleCodes.Documents_Block_PageReferenceTargetInvalid));
                break;
            case "width":
                writer.WriteNumberValue(Int(key, value, 80, 2000));
                break;
            case "caption":
                RichText.WriteRuns(writer, RichText.ParseRuns(value, allowMarks: true, "caption"));
                break;
            case "colWidths":
                WriteColumnWidths(writer, value);
                break;
            case "rows":
                WriteRows(writer, value);
                break;
            default:
                throw Invalid($"Unsupported property '{key}'.");
        }
    }

    private static void WriteColumnWidths(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > MaxTableColumns)
            throw Invalid($"colWidths must be an array of at most {MaxTableColumns} widths.");

        writer.WriteStartArray();
        foreach (var width in value.EnumerateArray())
        {
            if (width.ValueKind == JsonValueKind.Null) writer.WriteNullValue();
            else writer.WriteNumberValue(Int("colWidths", width, 40, 2000));
        }

        writer.WriteEndArray();
    }

    private static void WriteRows(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > MaxTableRows)
            throw Invalid($"rows must be an array of at most {MaxTableRows} rows.");

        var total = 0;
        writer.WriteStartArray();
        foreach (var row in value.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() > MaxTableColumns)
                throw Invalid($"A table row must be an array of at most {MaxTableColumns} cells.");

            writer.WriteStartArray();
            foreach (var cell in row.EnumerateArray())
            {
                if (cell.ValueKind != JsonValueKind.Object)
                    throw Invalid("A table cell must be an object.");

                IReadOnlyList<RichTextRun>? runs = null;
                string? background = null;
                var columnSpan = 1;
                var rowSpan = 1;
                foreach (var member in cell.EnumerateObject())
                {
                    switch (member.Name)
                    {
                        case "colSpan":
                            columnSpan = Int("colSpan", member.Value, 1, MaxTableColumns);
                            break;
                        case "rowSpan":
                            rowSpan = Int("rowSpan", member.Value, 1, MaxTableRows);
                            break;
                        case "runs":
                            runs = RichText.ParseRuns(member.Value, allowMarks: true, "A table cell's runs");
                            break;
                        case "bg":
                            background = OneOf("bg", member.Value, RichText.HighlightTokens);
                            break;
                        default:
                            throw Invalid($"A table cell has an unknown member '{member.Name}'.");
                    }
                }

                runs ??= [];
                total += RichText.PlainText(runs).Length;
                if (total > MaxTableTextLength)
                    throw new BusinessRuleException(DocumentRuleCodes.Documents_Block_ContentTooLarge, $"A table holds more than {MaxTableTextLength} characters of text.");

                writer.WriteStartObject();
                if (background is not null) writer.WriteString("bg", background);
                if (columnSpan > 1) writer.WriteNumber("colSpan", columnSpan);
                if (rowSpan > 1) writer.WriteNumber("rowSpan", rowSpan);
                writer.WritePropertyName("runs");
                RichText.WriteRuns(writer, runs);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }

    private static string OneOf(string key, JsonElement value, IReadOnlySet<string> allowed)
    {
        var text = value.ValueKind == JsonValueKind.String ? value.GetString()! : null;
        return text is not null && allowed.Contains(text)
            ? text
            : throw Invalid($"'{key}' must be one of: {string.Join(", ", allowed)}.");
    }

    private static bool Bool(string key, JsonElement value) =>
        value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : throw Invalid($"'{key}' must be true or false.");

    private static int Int(string key, JsonElement value, int min, int max) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number >= min && number <= max
            ? number
            : throw Invalid($"'{key}' must be a whole number from {min} to {max}.");

    private static string Text(string key, JsonElement value, int max, bool required)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw Invalid($"'{key}' must be text.");
        var text = value.GetString()!;
        if (text.Length > max || (required && text.Length == 0) || text.Any(char.IsControl))
            throw Invalid($"'{key}' must be {(required ? "1 to" : "at most")} {max} characters without control characters.");
        RichText.EnsureStorable(text, key);
        return text;
    }

    private static string Language(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw Invalid("'language' must be text.");
        var language = value.GetString()!.ToLowerInvariant();
        if (language.Length is 0 or > 32 || !language.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '#' or '-'))
            throw Invalid("'language' must be 1 to 32 characters of a-z, 0-9, +, # or -.");
        return language;
    }

    private static string Id(string key, JsonElement value, string ruleCode) =>
        value.ValueKind == JsonValueKind.String && Guid.TryParseExact(value.GetString(), "D", out var id) && id != Guid.Empty
            ? id.ToString()
            : throw new BusinessRuleException(ruleCode, $"'{key}' must be an id.");

    private static BusinessRuleException Invalid(string message) =>
        new(DocumentRuleCodes.Documents_Block_PropertiesInvalid, message);
}

/// <summary>
/// Content conversion for a block type change (the block keeps its id): rich text carries over between
/// text-like types; any other change starts empty.
/// </summary>
public static class BlockContentConversion
{
    private static readonly HashSet<BlockType> TextLike =
    [
        BlockType.Text, BlockType.Heading1, BlockType.Heading2, BlockType.Heading3,
        BlockType.BulletList, BlockType.NumberedList, BlockType.ToDo, BlockType.Toggle,
        BlockType.Quote, BlockType.Callout, BlockType.Code
    ];

    public static bool IsTextLike(BlockType type) => TextLike.Contains(type);

    public static BlockContent Convert(BlockType from, BlockType to, BlockContent content) =>
        IsTextLike(from) && IsTextLike(to) ? content : BlockContent.Create(JsonValue.EmptyObject());
}
