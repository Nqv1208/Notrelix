using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// One structured mention in a message's text (CHT-010): which characters of the text it covers and what they refer to. A person
/// and a channel are named by stable id, never by the text; the text is only where the chip sits. <see cref="Start"/> and
/// <see cref="Length"/> count UTF-16 characters of the stored (trimmed) content.
/// </summary>
public sealed record MentionSpan(MentionKind Kind, int Start, int Length, Guid? TargetId);

/// <summary>
/// The shape rules for the mentions of one message, held apart from who may be mentioned (that depends on the space and the
/// Workspace, so the handler decides it). A span is inside the text, spans do not overlap, a person and a channel carry an id, a
/// broadcast carries none, a person's text starts with "@", a channel link's with "#", and a broadcast is exactly "@channel" or
/// "@here". There are at most <see cref="ChatLimits.MentionsPerMessageMax"/> spans.
/// </summary>
public static class MentionSpans
{
    public const string ChannelToken = "@channel";
    public const string HereToken = "@here";

    /// <summary>
    /// Checks the spans a client sent against the text it sent and returns them ordered by position, with positions moved onto
    /// the trimmed text the message stores. A span that touches the trimmed whitespace, runs past the text, overlaps another span
    /// or does not fit its kind is refused with <see cref="ChatRuleCodes.MentionInvalid"/>; too many is
    /// <see cref="ChatRuleCodes.MentionLimitExceeded"/>.
    /// </summary>
    public static IReadOnlyList<MentionSpan> Normalize(string sentContent, IReadOnlyList<MentionSpan> spans)
    {
        Guard.NotNull(sentContent);
        Guard.NotNull(spans);
        if (spans.Count == 0) return [];
        if (spans.Count > ChatLimits.MentionsPerMessageMax)
            throw new BusinessRuleException(ChatRuleCodes.MentionLimitExceeded, $"A message has at most {ChatLimits.MentionsPerMessageMax} mentions.");

        var lead = sentContent.Length - sentContent.TrimStart().Length;
        var text = sentContent.Trim();

        var ordered = spans.OrderBy(s => s.Start).ToList();
        var result = new List<MentionSpan>(ordered.Count);
        var end = 0;
        foreach (var span in ordered)
        {
            var start = span.Start - lead;
            if (start < end || span.Length < 1 || start + span.Length > text.Length)
                throw Invalid("A mention is outside the text or overlaps another one.");

            var covered = text.Substring(start, span.Length);
            switch (span.Kind)
            {
                case MentionKind.User:
                    RequireId(span);
                    if (covered.Length < 2 || covered[0] != '@') throw Invalid("A person's mention covers text that starts with @.");
                    break;
                case MentionKind.ChannelLink:
                    RequireId(span);
                    if (covered.Length < 2 || covered[0] != '#') throw Invalid("A channel link covers text that starts with #.");
                    break;
                case MentionKind.BroadcastChannel:
                    RequireNoId(span);
                    if (!string.Equals(covered, ChannelToken, StringComparison.OrdinalIgnoreCase)) throw Invalid("A broadcast covers the text @channel.");
                    break;
                case MentionKind.BroadcastHere:
                    RequireNoId(span);
                    if (!string.Equals(covered, HereToken, StringComparison.OrdinalIgnoreCase)) throw Invalid("A broadcast covers the text @here.");
                    break;
                default:
                    throw Invalid("The mention kind is not known.");
            }

            result.Add(span with { Start = start });
            end = start + span.Length;
        }

        return result;
    }

    private static void RequireId(MentionSpan span)
    {
        if (span.TargetId is not { } id || id == Guid.Empty)
            throw Invalid("A person's mention and a channel link name their target by id.");
    }

    private static void RequireNoId(MentionSpan span)
    {
        if (span.TargetId is not null)
            throw Invalid("A broadcast names no target.");
    }

    private static BusinessRuleException Invalid(string message) => new(ChatRuleCodes.MentionInvalid, message);
}
