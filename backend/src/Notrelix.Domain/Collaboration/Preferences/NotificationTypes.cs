namespace Notrelix.Domain.Collaboration.Preferences;

/// <summary>
/// The stable vocabulary of notification types a person can set preferences for (accepted 2026-10-23).
/// The codes are persisted and public: never rename or reuse one. Mention, Assigned and Watching have a
/// producer today; the others are reserved so a person can already choose, and each gains delivery when its
/// producer exists.
/// </summary>
public static class NotificationTypes
{
    public const string Assigned = "assigned";
    public const string Mention = "mention";
    public const string Approval = "approval";
    public const string Overdue = "overdue";
    public const string Watching = "watching";
    public const string Invitation = "invitation";
    public const string Digest = "digest";

    /// <summary>The types in the order a client shows them.</summary>
    public static readonly IReadOnlyList<string> All =
        [Assigned, Mention, Approval, Overdue, Watching, Invitation, Digest];

    public static bool IsKnown(string? type) => type is not null && All.Contains(type, StringComparer.Ordinal);

    /// <summary>
    /// The notification type literal a producer stamps on a stored notification for a mention
    /// (<c>NotificationItemRecord.NotificationType</c>); it maps to <see cref="Mention"/>.
    /// </summary>
    public const string MentionProducedType = "mention.created";

    /// <summary>The Item the person is assigned to; maps to <see cref="Assigned"/>.</summary>
    public const string ItemAssignedProducedType = "work.item.assigned";

    /// <summary>Someone else was assigned to an Item the person follows; maps to <see cref="Watching"/>.</summary>
    public const string ItemAssigneeAddedProducedType = "work.item.assignee-added";

    /// <summary>An Item the person follows changed status; maps to <see cref="Watching"/>.</summary>
    public const string ItemStatusChangedProducedType = "work.item.status-changed";

    /// <summary>An Item the person follows got a comment; maps to <see cref="Watching"/>.</summary>
    public const string ItemCommentedProducedType = "work.item.commented";

    /// <summary>A message an Automation rule sent to a member about an Item; maps to <see cref="Assigned"/>.</summary>
    public const string AutomationProducedType = "work.item.automation";

    /// <summary>
    /// Maps the type literal a producer stores on a notification to the preference type, or null when the
    /// literal has no preference type.
    /// </summary>
    public static string? FromProducedType(string? producedType) => producedType switch
    {
        MentionProducedType => Mention,
        ItemAssignedProducedType or AutomationProducedType => Assigned,
        ItemAssigneeAddedProducedType or ItemStatusChangedProducedType or ItemCommentedProducedType => Watching,
        _ => null,
    };
}

/// <summary>
/// The stable channel codes of the preference matrix. Mobile and quiet hours are later decisions.
/// </summary>
public static class NotificationChannels
{
    public const string InApp = "in_app";
    public const string Email = "email";

    public static readonly IReadOnlyList<string> All = [InApp, Email];

    public static bool IsKnown(string? channel) => channel is not null && All.Contains(channel, StringComparer.Ordinal);
}

/// <summary>One requested cell of the matrix: set <paramref name="Type"/> on <paramref name="Channel"/> to <paramref name="Enabled"/>.</summary>
public readonly record struct NotificationPreferenceChange(string Type, string Channel, bool Enabled);

/// <summary>
/// The value of a cell nobody has changed (accepted 2026-10-23): in-app is on for every type; email is on
/// for assigned, mention, approval and invitation and off for overdue, watching and digest.
/// </summary>
public static class NotificationPreferenceDefaults
{
    public static bool IsEnabled(string type, string channel)
    {
        if (!NotificationTypes.IsKnown(type) || !NotificationChannels.IsKnown(channel))
            return false;

        if (channel == NotificationChannels.InApp)
            return true;

        return type is NotificationTypes.Assigned or NotificationTypes.Mention
            or NotificationTypes.Approval or NotificationTypes.Invitation;
    }
}
