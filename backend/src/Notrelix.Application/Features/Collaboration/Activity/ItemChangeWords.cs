using System.Globalization;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Public.ItemActivity;

namespace Notrelix.Application.Features.Collaboration.Activity;

/// <summary>
/// Words a field value change for the Item history: the before and after as short, readable text. It never
/// copies a whole document body or a sensitive value; a field whose values are hidden reports that it changed,
/// not what it changed to.
/// </summary>
public static class ItemChangeWords
{
    public const int MaxValueLength = 80;
    private const int MaxListedOptions = 5;

    public static (string? From, string? To) Describe(WorkFieldActivityFacts? field, string? oldRaw, string? newRaw)
    {
        if (field is null || field.ValueMode == WorkFieldValueMode.Hidden) return (null, null);
        return (Read(field, oldRaw), Read(field, newRaw));
    }

    private static string? Read(WorkFieldActivityFacts field, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            switch (field.ValueMode)
            {
                case WorkFieldValueMode.Option:
                    return root.ValueKind == JsonValueKind.String ? OptionName(field, root.GetString()) : null;
                case WorkFieldValueMode.OptionList:
                    if (root.ValueKind != JsonValueKind.Array) return null;
                    var names = root.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? OptionName(field, e.GetString()) : null)
                        .Where(n => n is not null)
                        .Cast<string>()
                        .ToList();
                    if (names.Count == 0) return null;
                    var listed = string.Join(", ", names.Take(MaxListedOptions));
                    return Truncate(names.Count > MaxListedOptions ? $"{listed} +{names.Count - MaxListedOptions}" : listed);
                default:
                    return root.ValueKind switch
                    {
                        JsonValueKind.String => Truncate(root.GetString()),
                        JsonValueKind.Number => root.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => null,
                    };
            }
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? OptionName(WorkFieldActivityFacts field, string? id) =>
        Guid.TryParse(id, CultureInfo.InvariantCulture, out var optionId)
        && field.OptionNames.TryGetValue(optionId, out var name)
            ? name
            : null;

    public static string? Truncate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var trimmed = text.Trim();
        return trimmed.Length <= MaxValueLength ? trimmed : string.Concat(trimmed.AsSpan(0, MaxValueLength - 1), "…");
    }
}
