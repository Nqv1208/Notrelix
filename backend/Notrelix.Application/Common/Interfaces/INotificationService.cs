namespace Notrelix.Application.Common.Interfaces;

/// <summary>
/// Interface cho notification service (Redis pub/sub + DB)
/// </summary>
public interface INotificationService
{
    Task SendAsync(Guid userId, string type, string payload, CancellationToken cancellationToken = default);
    Task SendToWorkspaceAsync(Guid workspaceId, string type, string payload, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default);
}
