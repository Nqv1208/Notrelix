namespace Notrelix.Domain.Collaboration.Preferences;

/// <summary>
/// One explicit choice of a person for a notification type on a channel, owned by its
/// <see cref="NotificationPreferenceSet"/>. A cell without an entry has the default value.
/// </summary>
public sealed class NotificationPreferenceEntry : Entity
{
    public Guid? SetId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; } = null!;
    public string Channel { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private NotificationPreferenceEntry() : base() { }

    internal static NotificationPreferenceEntry Create(
        NotificationPreferenceSet set, string type, string channel, bool enabled, DateTimeOffset now) =>
        new()
        {
            SetId = set.Id,
            WorkspaceId = set.WorkspaceId,
            UserId = set.UserId,
            NotificationType = type,
            Channel = channel,
            IsEnabled = enabled,
            CreatedAt = now,
        };

    internal void Set(bool enabled, DateTimeOffset now)
    {
        IsEnabled = enabled;
        UpdatedAt = now;
    }
}
