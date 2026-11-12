using System.Globalization;
using System.Net;
using System.Text;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.ChatDigest;

/// <summary>One line of a digest: where the message was written, why the person hears of it, a short excerpt and the link that opens the space.</summary>
public sealed record ChatDigestLine(string? SpaceName, string Reason, string? Excerpt, string Url);

/// <summary>The words of one digest email.</summary>
public sealed record ChatDigestEmail(string Subject, string BodyHtml, string BodyText);

/// <summary>
/// Renders the missed-mention digest (UC-CHT-H09): a short list of excerpts the recipient may still see, each with a link to the space, and
/// "and N more" when the person has more than the digest shows. Plain text and HTML (everything encoded), in the person's language (vi or en;
/// any other language reads English). It carries no author name and no attachment; excerpts are the notification excerpt (links, markup and
/// emphasis removed, at most 200 characters).
/// </summary>
public static class ChatDigestMessage
{
    public static ChatDigestEmail Render(string? locale, IReadOnlyList<ChatDigestLine> lines, int more, string settingsUrl)
    {
        var words = IsVietnamese(locale) ? Vi : En;
        var total = lines.Count + more;
        var subject = total == 1 ? words.SubjectOne : string.Format(CultureInfo.InvariantCulture, words.SubjectMany, total);

        var text = new StringBuilder();
        var html = new StringBuilder();
        text.AppendLine(words.Intro).AppendLine();
        html.Append("<p>").Append(WebUtility.HtmlEncode(words.Intro)).Append("</p><ul>");
        foreach (var line in lines)
        {
            var where = line.SpaceName is { Length: > 0 } name ? "#" + name : words.Conversation;
            var excerpt = NotificationExcerpt.From(line.Excerpt) ?? words.NoExcerpt;
            var reason = words.Reason(line.Reason);
            text.Append("- ").Append(where).Append(" (").Append(reason).Append("): ").AppendLine(excerpt).Append("  ").AppendLine(line.Url);
            html.Append("<li><strong>").Append(WebUtility.HtmlEncode(where)).Append("</strong> (").Append(WebUtility.HtmlEncode(reason)).Append(") ")
                .Append(WebUtility.HtmlEncode(excerpt)).Append(" <a href=\"").Append(WebUtility.HtmlEncode(line.Url)).Append("\">")
                .Append(WebUtility.HtmlEncode(words.Open)).Append("</a></li>");
        }

        html.Append("</ul>");
        if (more > 0)
        {
            var moreText = string.Format(CultureInfo.InvariantCulture, words.More, more);
            text.AppendLine().AppendLine(moreText);
            html.Append("<p>").Append(WebUtility.HtmlEncode(moreText)).Append("</p>");
        }

        text.AppendLine().Append(words.Settings).Append(' ').AppendLine(settingsUrl);
        html.Append("<p>").Append(WebUtility.HtmlEncode(words.Settings)).Append(" <a href=\"").Append(WebUtility.HtmlEncode(settingsUrl)).Append("\">")
            .Append(WebUtility.HtmlEncode(settingsUrl)).Append("</a></p>");

        return new ChatDigestEmail(subject, html.ToString(), text.ToString());
    }

    private static bool IsVietnamese(string? locale) =>
        locale is not null && locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

    private sealed record Words(
        string SubjectOne, string SubjectMany, string Intro, string Conversation, string NoExcerpt, string Open, string More, string Settings,
        string ReasonMention, string ReasonDirect, string ReasonThread, string ReasonBroadcast)
    {
        public string Reason(string reason) => reason switch
        {
            ChatNotificationReasons.Mention => ReasonMention,
            ChatNotificationReasons.DirectMessage => ReasonDirect,
            ChatNotificationReasons.ThreadReply => ReasonThread,
            _ => ReasonBroadcast,
        };
    }

    private static readonly Words En = new(
        "You missed a chat message",
        "You missed {0} chat messages",
        "You have chat messages you have not read yet:",
        "Direct message",
        "(no text)",
        "Open",
        "and {0} more",
        "Change how you get this digest in your notification settings:",
        "mentioned you",
        "message to you",
        "reply in a thread you follow",
        "message to everyone");

    private static readonly Words Vi = new(
        "Bạn bỏ lỡ một tin nhắn trò chuyện",
        "Bạn bỏ lỡ {0} tin nhắn trò chuyện",
        "Bạn có tin nhắn trò chuyện chưa đọc:",
        "Tin nhắn trực tiếp",
        "(không có nội dung)",
        "Mở",
        "và {0} tin nữa",
        "Đổi cách nhận bản tóm tắt này trong cài đặt thông báo:",
        "nhắc đến bạn",
        "gửi cho bạn",
        "trả lời trong chuỗi bạn theo dõi",
        "gửi cho mọi người");
}
