using System.Globalization;
using System.Text;
using Notrelix.Application.Common.Search;
using Notrelix.Application.Features.Chat.Search.DTOs;

namespace Notrelix.Application.Features.Chat.Search.Services;

/// <summary>
/// Builds the excerpt of a hit: a window of the message around its first match and the ranges of every match inside the window. The match
/// ignores marks and case the way <see cref="SearchTextFolding"/> does, and the ranges are positions in the author's original text (so a
/// message stored with combining marks still highlights the right letters). The result is plain text and numbers; nothing is escaped or
/// wrapped because nothing is HTML.
/// </summary>
public static class ChatSearchSnippets
{
    public const int Before = 40;
    public const int After = 100;

    public static ChatSearchSnippetDto Build(string content, string typed)
    {
        var needle = SearchTextFolding.Fold(SearchTextFolding.Clean(typed));
        var (folded, origin) = FoldWithMap(content);
        var matches = new List<(int Start, int End)>();
        if (needle.Length > 0)
        {
            var at = 0;
            while (at <= folded.Length - needle.Length)
            {
                var found = folded.IndexOf(needle, at, StringComparison.Ordinal);
                if (found < 0) break;
                matches.Add((origin[found], origin[found + needle.Length - 1] + 1));
                at = found + needle.Length;
            }
        }

        var first = matches.Count > 0 ? matches[0] : (Start: 0, End: 0);
        var start = Math.Max(0, first.Start - Before);
        var end = Math.Min(content.Length, Math.Max(first.End, first.Start) + After);
        if (start > 0 && char.IsLowSurrogate(content[start])) start--;
        if (end < content.Length && char.IsLowSurrogate(content[end])) end++;
        // Combining marks belong to the letter before them; keep them together.
        while (start > 0 && start < content.Length && CharUnicodeInfo.GetUnicodeCategory(content[start]) == UnicodeCategory.NonSpacingMark) start--;
        while (end < content.Length && CharUnicodeInfo.GetUnicodeCategory(content[end]) == UnicodeCategory.NonSpacingMark) end++;

        var ranges = matches
            .Where(m => m.Start >= start && m.End <= end)
            .Select(m => new ChatSearchRangeDto(m.Start - start, m.End - m.Start))
            .ToList();
        return new ChatSearchSnippetDto(content[start..end], ranges, start > 0, end < content.Length);
    }

    /// <summary>The folded text and, for each folded character, where it sits in the original (marks fold away, so the two differ in length).</summary>
    private static (string Folded, int[] Origin) FoldWithMap(string content)
    {
        var builder = new StringBuilder(content.Length);
        var origin = new List<int>(content.Length);
        for (var i = 0; i < content.Length; i++)
        {
            // A surrogate half cannot be normalized alone; it never matches a letter, so it stays as it is.
            var folded = char.IsSurrogate(content[i]) ? content[i].ToString() : SearchTextFolding.Fold(content[i].ToString());
            foreach (var character in folded)
            {
                builder.Append(character);
                origin.Add(i);
            }
        }

        return (builder.ToString(), [.. origin]);
    }
}
