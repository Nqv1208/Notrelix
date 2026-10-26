using System.Text.RegularExpressions;

namespace Notrelix.Application.Features.Collaboration.Notifications;

/// <summary>
/// The plain-text excerpt a notification stores of the text that caused it: no markup, one line, at most
/// <see cref="Length"/> characters. A comment is Markdown, so links keep their label and the emphasis marks
/// are dropped.
/// </summary>
public static class NotificationExcerpt
{
    public const int Length = 200;

    private static readonly Regex Link = new(@"!?\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled);
    private static readonly Regex Markup = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex LinePrefix = new(@"^[ \t]*[#>]+[ \t]*", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex Emphasis = new(@"[*`]+|~~|__", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static string? From(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var plain = Link.Replace(text, "$1");
        plain = Markup.Replace(plain, " ");
        plain = LinePrefix.Replace(plain, string.Empty);
        plain = Emphasis.Replace(plain, string.Empty);
        plain = Whitespace.Replace(plain, " ").Trim();
        if (plain.Length == 0) return null;
        return plain.Length <= Length ? plain : plain[..(Length - 1)] + "…";
    }
}
