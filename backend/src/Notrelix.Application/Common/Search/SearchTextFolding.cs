using System.Globalization;
using System.Text;

namespace Notrelix.Application.Common.Search;

/// <summary>
/// Mark-insensitive, case-insensitive text matching for search ("hoa don" finds "Hoá đơn"). A typed text is
/// folded (no combining marks, "đ" as "d", lower case) and turned into a regular expression whose letters accept
/// every accented form of themselves, so a datastore can match stored text without an extension or a second
/// copy of it. The pattern is for the POSIX/.NET regular-expression match used by <c>Regex.IsMatch</c>; it is
/// built from the typed text only after escaping, so typed text can never change the pattern's meaning.
/// </summary>
public static class SearchTextFolding
{
    /// <summary>The shortest typed text a search answers.</summary>
    public const int MinLength = 2;

    /// <summary>The longest typed text a search uses; longer input is cut.</summary>
    public const int MaxLength = 100;

    private static readonly Lazy<IReadOnlyDictionary<char, string>> Variants = new(BuildVariants);

    /// <summary>Lower case, without combining marks, "đ" as "d".</summary>
    public static string Fold(string text)
    {
        var decomposed = text.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>The typed text, trimmed, white space collapsed and cut to <see cref="MaxLength"/>.</summary>
    public static string Clean(string? typed)
    {
        if (string.IsNullOrWhiteSpace(typed)) return string.Empty;
        var collapsed = string.Join(' ', typed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Length > MaxLength ? collapsed[..MaxLength].TrimEnd() : collapsed;
    }

    /// <summary>
    /// A regular expression matching the typed text inside other text, whatever the marks and the case of either.
    /// Empty when the cleaned text is shorter than <see cref="MinLength"/>.
    /// </summary>
    public static string ToPattern(string? typed)
    {
        var folded = Fold(Clean(typed));
        if (folded.Length < MinLength) return string.Empty;

        var builder = new StringBuilder(folded.Length * 8);
        foreach (var character in folded)
        {
            if (Variants.Value.TryGetValue(character, out var forms))
            {
                builder.Append('[').Append(forms).Append(']');
            }
            else if (character is '\\' or '^' or '$' or '.' or '|' or '?' or '*' or '+' or '(' or ')' or '[' or ']' or '{' or '}')
            {
                builder.Append('\\').Append(character);
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    /// <summary>True when the text contains the typed text, ignoring marks and case.</summary>
    public static bool Contains(string? text, string? typed) =>
        !string.IsNullOrEmpty(text) && Fold(text).Contains(Fold(Clean(typed)), StringComparison.Ordinal);

    private static IReadOnlyDictionary<char, string> BuildVariants()
    {
        var byBase = new Dictionary<char, StringBuilder>();
        void Add(char baseLetter, char form)
        {
            if (!byBase.TryGetValue(baseLetter, out var forms))
                byBase[baseLetter] = forms = new StringBuilder();
            if (forms.ToString().IndexOf(form) < 0) forms.Append(form);
        }

        // Every letter accepts itself in both cases, then each accented form of it (Latin-1, Latin Extended,
        // Vietnamese additions); "đ" is a form of "d".
        for (var letter = 'a'; letter <= 'z'; letter++)
        {
            Add(letter, letter);
            Add(letter, char.ToUpperInvariant(letter));
        }

        for (var code = 0x00C0; code <= 0x024F; code++) AddForm((char)code);
        for (var code = 0x1E00; code <= 0x1EFF; code++) AddForm((char)code);
        Add('d', 'đ');
        Add('d', 'Đ');
        return byBase.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());

        void AddForm(char form)
        {
            var folded = Fold(form.ToString());
            if (folded.Length == 1 && folded[0] is >= 'a' and <= 'z' && folded[0] != form) Add(folded[0], form);
        }
    }
}
