namespace Notrelix.Application.Common.PostCommit;

public interface IRealtimePublisher
{
    Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken);
}
