using System.Runtime.CompilerServices;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Operations;

public sealed class LatestReplayStrategy : ReplayStrategyBase
{
    public override ReplayStrategyType StrategyType => ReplayStrategyType.Latest;

    public override async IAsyncEnumerable<EventPublication> GetEventsAsync(
        ReplayRequest request,
        IReplayCheckpointStore checkpointStore,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var checkpoint = await checkpointStore.GetLatestAsync(request.EventName, request.WorkspaceId, cancellationToken);

        if (checkpoint is null)
            yield break;

        // Position-based enumeration happens in the concrete source implementation.
        // Platform strategy provides the bounds — Infrastructure provides the events.
        yield break;
    }
}
