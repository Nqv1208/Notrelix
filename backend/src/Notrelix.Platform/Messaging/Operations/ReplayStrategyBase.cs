using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Operations;

public abstract class ReplayStrategyBase : IReplayStrategy
{
    public abstract ReplayStrategyType StrategyType { get; }

    public abstract IAsyncEnumerable<EventPublication> GetEventsAsync(
        ReplayRequest request,
        IReplayCheckpointStore checkpointStore,
        CancellationToken cancellationToken = default);

    protected static async IAsyncEnumerable<EventPublication> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }
}
