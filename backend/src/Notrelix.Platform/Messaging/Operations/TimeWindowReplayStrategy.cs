using System.Runtime.CompilerServices;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Operations;

public sealed class TimeWindowReplayStrategy : ReplayStrategyBase
{
    public override ReplayStrategyType StrategyType => ReplayStrategyType.TimeWindow;

    public override async IAsyncEnumerable<EventPublication> GetEventsAsync(
        ReplayRequest request,
        IReplayCheckpointStore checkpointStore,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }
}
