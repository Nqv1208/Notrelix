namespace Notrelix.Application.Common.Abstractions;

public interface IRealtimePublisher
{
    Task PublishAsync(RealtimeTopic topic, object payload, CancellationToken cancellationToken);
}
