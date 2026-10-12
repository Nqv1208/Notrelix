using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Notrelix.Application.Features.WorkManagement.Common;

/// <summary>
/// Read-side resolution of the Board fields whose values the board read contract
/// surfaces as first-class Item values. It never changes how values are written.
/// <list type="bullet">
/// <item>Status: the first non-deleted Status field by position (then id).</item>
/// <item>Priority: the first non-deleted Select field by position (then id) whose
/// name, trimmed, lower-cased, with spaces/dashes as underscores and Vietnamese
/// diacritics removed, is <c>priority</c>, <c>uu_tien</c>, <c>do_uu_tien</c> or
/// <c>muc_uu_tien</c>.</item>
/// </list>
/// </summary>
public static class WorkItemSemanticFields
{
    private static readonly HashSet<string> PriorityNames = new(StringComparer.Ordinal)
    {
        "priority", "uu_tien", "do_uu_tien", "muc_uu_tien",
    };

    public static BoardField? ResolveStatusField(IEnumerable<BoardField> fields) =>
        Ordered(fields).FirstOrDefault(f => f.Type == FieldType.Status);

    public static BoardField? ResolvePriorityField(IEnumerable<BoardField> fields) =>
        Ordered(fields).FirstOrDefault(f => f.Type == FieldType.Select && PriorityNames.Contains(NormalizeName(f.Name)));

    /// <summary>The option id held by a single-option (Select/Status) value, or null.</summary>
    public static Guid? ReadOptionId(FieldValue? value)
    {
        if (value is null || value.IsEmpty) return null;
        try
        {
            using var document = JsonDocument.Parse(value.Data.Value);
            return document.RootElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(document.RootElement.GetString(), out var id)
                ? id
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static string NormalizeName(string name)
    {
        var decomposed = name.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(character is ' ' or '-' ? '_' : character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static IEnumerable<BoardField> Ordered(IEnumerable<BoardField> fields) =>
        fields.Where(f => f.DeletedAt == null).OrderBy(f => f.Position).ThenBy(f => f.Id);
}
