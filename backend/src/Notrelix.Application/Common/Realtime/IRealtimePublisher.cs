namespace Notrelix.Application.Common.Realtime;

public interface IRealtimePublisher
{
    Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken);
}
