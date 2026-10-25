namespace Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;

/// <summary>
/// Work Management-published facts about one Item that a follower notification needs: where it lives,
/// what it is called and, when a Status field changed, the status it now has. Collaboration composes the
/// words of the notification; it never reads Work persistence.
/// </summary>
public interface IWorkItemNotificationFacts
{
    /// <summary>
    /// The Item's facts, or null when it no longer exists in the Workspace. <paramref name="changedFieldId"/>
    /// is the field a change event reported, when there is one.
    /// </summary>
    Task<WorkItemNotificationFacts?> FindAsync(
        Guid workspaceId,
        Guid itemId,
        Guid? changedFieldId,
        CancellationToken cancellationToken);
}

/// <param name="ChangedFieldIsStatus">True when the reported field is the Board's Status field.</param>
/// <param name="StatusName">The name of the status the Item holds now, when it has one.</param>
public sealed record WorkItemNotificationFacts(
    Guid BoardId,
    string Title,
    bool ChangedFieldIsStatus,
    string? StatusName);
