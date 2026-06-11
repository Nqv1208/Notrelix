namespace Notrelix.Application.Common.Abstractions;

/// <summary>
/// Interface cho đồng bộ với external calendar (Google Calendar, Outlook)
/// </summary>
public interface ICalendarSyncService
{
    Task SyncCardAsync(Guid cardId, CancellationToken cancellationToken = default);
    Task SyncPageAsync(Guid pageId, CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string provider, string payload, CancellationToken cancellationToken = default);
}
