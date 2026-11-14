namespace Notrelix.Application.Features.WorkManagement.Public.FormNotifications;

/// <summary>
/// Work Management-published facts about one form that a "new response" notification needs: its Board, its
/// name and the people who manage it. Collaboration composes the words of the notification; it never reads
/// Work persistence.
/// </summary>
public interface IFormNotificationFacts
{
    /// <summary>The form's facts, or null when the form or its Board no longer exists in the Workspace.</summary>
    Task<FormNotificationFacts?> FindAsync(Guid workspaceId, Guid formId, CancellationToken cancellationToken);
}

/// <param name="ManagerUserIds">
/// Who manages the form: the person who created it and the Board's managers (board Owner or Admin members).
/// Whether each may still see the Board is checked by the reader of these facts.
/// </param>
public sealed record FormNotificationFacts(
    Guid BoardId,
    string FormName,
    IReadOnlyList<Guid> ManagerUserIds);
