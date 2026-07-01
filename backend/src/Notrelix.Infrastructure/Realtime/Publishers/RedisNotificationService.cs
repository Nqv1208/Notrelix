using System.Text.Json;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Realtime.Publishers;

public class RedisNotificationService : INotificationService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisNotificationService(IConnectionMultiplexer redis)
    {
        _redis = redis;
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

    public Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task MarkAllAsReadAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
