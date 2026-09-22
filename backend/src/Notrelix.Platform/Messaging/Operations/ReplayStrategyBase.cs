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

    /// <summary>
    /// The platform owns replay orchestration, not an event store. A concrete
    /// strategy must not silently complete with zero events when no retained
    /// payload source has been supplied by the owning infrastructure.
    /// </summary>
    protected static void EnsureRetainedEventSource(ReplayRequest request)
    {
        throw new ReplaySourceUnavailableException(
            $"Replay strategy '{request.StrategyType}' requires a declared retained event source for '{request.EventName}'.");
    }
}

public sealed class ReplaySourceUnavailableException(string message) : InvalidOperationException(message);
