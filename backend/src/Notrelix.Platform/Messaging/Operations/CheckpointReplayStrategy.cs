using System.Runtime.CompilerServices;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Operations;

public sealed class CheckpointReplayStrategy : ReplayStrategyBase
{
    public override ReplayStrategyType StrategyType => ReplayStrategyType.Checkpoint;

    public override async IAsyncEnumerable<EventPublication> GetEventsAsync(
        ReplayRequest request,
        IReplayCheckpointStore checkpointStore,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request.CheckpointId is null)
            yield break;

        var checkpoint = await checkpointStore.GetByIdAsync(request.CheckpointId.Value, cancellationToken);

        if (checkpoint is null)
            yield break;

        yield break;
    }
}
