namespace Notrelix.Application.Common.PostCommit;

public interface IRealtimePublisher
{
    Task PublishAsync(RealtimeTopic topic, object payload, CancellationToken cancellationToken);
}
