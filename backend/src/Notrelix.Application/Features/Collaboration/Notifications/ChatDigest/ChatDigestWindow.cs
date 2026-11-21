namespace Notrelix.Application.Features.Collaboration.Notifications.ChatDigest;

/// <summary>
/// One digest window (UC-CHT-H09): a fixed span of UTC time that starts at a multiple of its length since the epoch, so every instance and
/// every re-run agree on it. A digest covers the notifications created inside one completed window and is sent once per person and
/// Workspace per window: <see cref="Key"/> is part of the email's deduplication key, so a repeated pass queues nothing twice.
/// </summary>
public readonly record struct ChatDigestWindow(DateTimeOffset Start, DateTimeOffset End)
{
    public static readonly TimeSpan MinLength = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxLength = TimeSpan.FromDays(7);

    public string Key => Start.UtcDateTime.ToString("yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The latest window that has ended by <paramref name="now"/>.</summary>
    public static ChatDigestWindow Latest(DateTimeOffset now, TimeSpan length)
    {
        if (length < MinLength || length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(length), length, "The digest window is 1 minute to 7 days.");

        var ticks = now.UtcTicks / length.Ticks * length.Ticks;
        var end = new DateTimeOffset(ticks, TimeSpan.Zero);
        return new ChatDigestWindow(end - length, end);
    }

    /// <summary>
    /// The latest window and the <paramref name="catchUp"/> windows before it, oldest first. A pass that covers more than the latest window
    /// sends a window the job missed (it was down, or failed): the per-window deduplication key keeps a window already sent from going twice.
    /// </summary>
    public static IReadOnlyList<ChatDigestWindow> Recent(DateTimeOffset now, TimeSpan length, int catchUp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(catchUp);
        return Enumerable.Range(0, catchUp + 1).Reverse().Select(back => Latest(now - back * length, length)).ToList();
    }

    public string DeduplicationKey(Guid workspaceId, Guid userId) => $"chat-digest:{workspaceId:N}:{userId:N}:{Key}";
}
