using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Operations;

public interface IReplayStrategy
{
    ReplayStrategyType StrategyType { get; }

    IAsyncEnumerable<EventPublication> GetEventsAsync(
        ReplayRequest request,
        IReplayCheckpointStore checkpointStore,
        CancellationToken cancellationToken = default);
}
