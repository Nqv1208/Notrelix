using StackExchange.Redis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Realtime.Publishers;

public class RedisNotificationService : INotificationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IApplicationDbContext _context;

    public RedisNotificationService(IConnectionMultiplexer redis, IApplicationDbContext context)
    {
        _redis = redis;
        _context = context;
    }

    public async Task SendAsync(Guid userId, string type, string payload, CancellationToken cancellationToken = default)
    {
        var subscriber = _redis.GetSubscriber();

        var message = new
        {
            userId,
            type,
            payload,
            createdAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(message);

        await subscriber.PublishAsync(new RedisChannel($"notifications:{userId}", RedisChannel.PatternMode.Literal), json);
    }

    public async Task SendToWorkspaceAsync(Guid workspaceId, string type, string payload, CancellationToken cancellationToken = default)
    {
        var subscriber = _redis.GetSubscriber();

        var message = new
        {
            workspaceId,
            type,
            payload,
            createdAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(message);

        await subscriber.PublishAsync(new RedisChannel($"notifications:workspace:{workspaceId}", RedisChannel.PatternMode.Literal), json);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification != null)
        {
            notification.MarkAsRead(DateTimeOffset.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.WorkspaceId == workspaceId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead(DateTimeOffset.UtcNow);
        }

        if (unreadNotifications.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
