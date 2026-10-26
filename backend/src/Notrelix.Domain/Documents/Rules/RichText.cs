using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Notrelix.Domain.Documents.Rules;

/// <summary>One mark on a run: <c>Value</c> is the href, token, user id or page id the type carries.</summary>
public sealed record RichTextMark(string Type, string? Value = null);

/// <summary>A piece of text with the marks that apply to all of it.</summary>
public sealed record RichTextRun(string Text, IReadOnlyList<RichTextMark> Marks);

/// <summary>
/// Rich text v1 (DCT-007): the authored text of a text-like Block as ordered runs with a closed set of
/// marks. This type owns parsing, validation, the canonical normal form and link sanitising; the byte
/// output is deterministic so equal text compares equal as stored JSON.
/// </summary>
public static class RichText
{
    public const int Version = 1;
    public const int MaxTextLength = 20_000;
    public const int MaxRuns = 5_000;
    public const int MaxHrefLength = 2_048;

    public const string Bold = "bold";
    public const string Italic = "italic";
    public const string Underline = "underline";
    public const string Strike = "strike";
    public const string Code = "code";
    public const string Superscript = "superscript";
    public const string Subscript = "subscript";
    public const string Link = "link";
    public const string Color = "color";
    public const string Highlight = "highlight";
    public const string Mention = "mention";
    public const string PageRef = "pageRef";

    private static readonly HashSet<string> SimpleMarks = [Bold, Italic, Underline, Strike, Code, Superscript, Subscript];

    public static readonly IReadOnlySet<string> ColorTokens = new HashSet<string>
        { "default", "muted", "primary", "accent", "success", "warning", "danger" };

    public static readonly IReadOnlySet<string> HighlightTokens = new HashSet<string>
        { "none", "muted", "accent", "primary", "success", "warning", "danger" };

    /// <summary>Stored JSON is data, not markup: characters stay readable instead of becoming \u escapes.</summary>
    internal static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly HashSet<string> SafeSchemes = ["http", "https", "mailto"];

    /// <summary>The runs array of a Block (or caption/cell) as normalised runs.</summary>
    public static IReadOnlyList<RichTextRun> ParseRuns(JsonElement runs, bool allowMarks, string path)
    {
        if (runs.ValueKind != JsonValueKind.Array)
            throw Invalid($"{path} must be an array of runs.");
        if (runs.GetArrayLength() > MaxRuns)
            throw TooLarge($"{path} has more than {MaxRuns} runs.");

        var parsed = new List<RichTextRun>(runs.GetArrayLength());
        var total = 0;
        foreach (var run in runs.EnumerateArray())
        {
            if (run.ValueKind != JsonValueKind.Object)
                throw Invalid($"{path} must contain run objects.");

            string? text = null;
            IReadOnlyList<RichTextMark> marks = [];
            foreach (var member in run.EnumerateObject())
            {
                switch (member.Name)
                {
                    case "text":
                        if (member.Value.ValueKind != JsonValueKind.String)
                            throw Invalid("A run's text must be a string.");
                        text = member.Value.GetString()!;
                        break;
                    case "marks":
                        marks = ParseMarks(member.Value);
                        break;
                    default:
                        throw Invalid($"A run has an unknown member '{member.Name}'.");
                }
            }

            if (text is null)
                throw Invalid("A run needs its text.");
            EnsureStorable(text, "A run's text");

            total += text.Length;
            if (total > MaxTextLength)
                throw TooLarge($"{path} holds more than {MaxTextLength} characters of text.");

            parsed.Add(new RichTextRun(text, allowMarks ? marks : []));
        }

        return Normalize(parsed);
    }

    private static List<RichTextMark> ParseMarks(JsonElement marks)
    {
        if (marks.ValueKind != JsonValueKind.Array)
            throw Invalid("A run's marks must be an array.");
        if (marks.GetArrayLength() > 32)
            throw Invalid("A run has too many marks.");

        var parsed = new List<RichTextMark>();
        foreach (var mark in marks.EnumerateArray())
        {
            if (mark.ValueKind != JsonValueKind.Object)
                throw Mark("A mark must be an object.");
            if (!mark.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                throw Mark("A mark needs a type.");

            var type = typeElement.GetString()!;
            if (SimpleMarks.Contains(type))
            {
                RejectOtherMembers(mark, type);
                parsed.Add(new RichTextMark(type));
                continue;
            }

            string attribute;
            Func<string, string> validate;
            switch (type)
            {
                case Link: attribute = "href"; validate = SanitiseHref; break;
                case Color: attribute = "token"; validate = v => EnsureToken(v, ColorTokens, "color"); break;
                case Highlight: attribute = "token"; validate = v => EnsureToken(v, HighlightTokens, "highlight"); break;
                case Mention: attribute = "userId"; validate = v => EnsureGuid(v, "mention userId"); break;
                case PageRef: attribute = "pageId"; validate = v => EnsureGuid(v, "pageRef pageId"); break;
                default: throw Mark($"Unknown mark type '{type}'.");
            }

            RejectOtherMembers(mark, type, attribute);
            if (!mark.TryGetProperty(attribute, out var value) || value.ValueKind != JsonValueKind.String)
                throw Mark($"A '{type}' mark needs a string '{attribute}'.");
            parsed.Add(new RichTextMark(type, validate(value.GetString()!)));
        }

        return parsed;
    }

    private static void RejectOtherMembers(JsonElement mark, string type, string? attribute = null)
    {
        foreach (var member in mark.EnumerateObject())
        {
            if (member.Name != "type" && member.Name != attribute)
                throw Mark($"A '{type}' mark has an unknown member '{member.Name}'.");
        }
    }

    /// <summary>
    /// A link target is an absolute http, https or mailto address of at most 2048 characters; everything
    /// else (javascript:, data:, relative and scheme-less values) is rejected, never rewritten.
    /// </summary>
    public static string SanitiseHref(string href)
    {
        var value = href.Trim();
        if (value.Length == 0 || value.Length > MaxHrefLength || value.Any(char.IsControl) || value.Any(char.IsWhiteSpace))
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Block_LinkHrefInvalid, "A link must be a http, https or mailto address of at most 2048 characters.");

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !SafeSchemes.Contains(uri.Scheme))
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Block_LinkHrefInvalid, "A link must be a http, https or mailto address.");

        var hasTarget = uri.Scheme == "mailto"
            ? value.Length > "mailto:".Length
            : !string.IsNullOrEmpty(uri.Host);
        if (!hasTarget)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Block_LinkHrefInvalid, "A link needs an address to point at.");

        return value;
    }

    /// <summary>
    /// Canonical form: empty runs dropped, duplicate mark types collapsed (last wins), marks ordered by type,
    /// <c>code</c> excludes every mark but <c>link</c>, <c>superscript</c> excludes <c>subscript</c>, adjacent runs with the same marks merged.
    /// </summary>
    public static IReadOnlyList<RichTextRun> Normalize(IEnumerable<RichTextRun> runs)
    {
        var result = new List<RichTextRun>();
        foreach (var run in runs)
        {
            if (run.Text.Length == 0) continue;

            var byType = new Dictionary<string, RichTextMark>(StringComparer.Ordinal);
            foreach (var mark in run.Marks) byType[mark.Type] = mark;
            if (byType.ContainsKey(Code))
            {
                foreach (var type in byType.Keys.Where(t => t != Code && t != Link).ToList())
                    byType.Remove(type);
            }

            // Raised and lowered text exclude each other; raised wins.
            if (byType.ContainsKey(Superscript)) byType.Remove(Subscript);

            var marks = byType.Values.OrderBy(m => m.Type, StringComparer.Ordinal).ToList();
            if (result.Count > 0 && result[^1].Marks.SequenceEqual(marks))
            {
                result[^1] = result[^1] with { Text = result[^1].Text + run.Text };
                continue;
            }

            result.Add(new RichTextRun(run.Text, marks));
        }

        return result;
    }

    public static void WriteRuns(Utf8JsonWriter writer, IReadOnlyList<RichTextRun> runs)
    {
        writer.WriteStartArray();
        foreach (var run in runs)
        {
            writer.WriteStartObject();
            writer.WriteString("text", run.Text);
            writer.WriteStartArray("marks");
            foreach (var mark in run.Marks)
            {
                writer.WriteStartObject();
                writer.WriteString("type", mark.Type);
                if (mark.Value is not null)
                {
                    writer.WriteString(mark.Type switch
                    {
                        Link => "href",
                        Mention => "userId",
                        PageRef => "pageId",
                        _ => "token",
                    }, mark.Value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static string ToJson(IReadOnlyList<RichTextRun> runs)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteNumber("v", Version);
            writer.WritePropertyName("runs");
            WriteRuns(writer, runs);
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>Plain text of runs (what search and previews read).</summary>
    public static string PlainText(IEnumerable<RichTextRun> runs) => string.Concat(runs.Select(r => r.Text));

    /// <summary>jsonb cannot store NUL, and lone surrogates do not survive UTF-8.</summary>
    internal static void EnsureStorable(string value, string what)
    {
        if (value.Contains('\0'))
            throw Invalid($"{what} cannot contain a NUL character.");
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) { i++; continue; }
            if (char.IsSurrogate(value[i]))
                throw Invalid($"{what} contains invalid Unicode.");
        }
    }

    private static string EnsureToken(string value, IReadOnlySet<string> allowed, string what) =>
        allowed.Contains(value) ? value : throw Mark($"'{value}' is not a {what} token.");

    private static string EnsureGuid(string value, string what) =>
        Guid.TryParseExact(value, "D", out var id) && id != Guid.Empty
            ? id.ToString()
            : throw Mark($"{what} must be an id.");

    private static BusinessRuleException Invalid(string message) =>
        new(DocumentRuleCodes.Documents_Block_ContentInvalid, message);

    private static BusinessRuleException Mark(string message) =>
        new(DocumentRuleCodes.Documents_Block_MarkInvalid, message);

    private static BusinessRuleException TooLarge(string message) =>
        new(DocumentRuleCodes.Documents_Block_ContentTooLarge, message);
}
