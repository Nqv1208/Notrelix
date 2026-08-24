namespace Notrelix.Infrastructure.Messaging;

public sealed class DevNullRealtimePublisher : IRealtimePublisher
{
    public Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
