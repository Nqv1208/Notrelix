using System.Text.Json;

namespace Notrelix.Application.Common.Realtime;

public abstract class RealtimeChangeMapper<TRequest, TResponse> : IRealtimeChangeMapper<TRequest, TResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IExecutionContextReader _context;
    private readonly IDateTimeProvider _time;

    protected RealtimeChangeMapper(IExecutionContextReader context, IDateTimeProvider time)
    {
        _context = context;
        _time = time;
    }

    public abstract RealtimeResourceChangedV1 Map(TRequest request, TResponse response, long streamVersion);

    protected RealtimeResourceChangedV1 Create(
        string topicNamespace,
        string resourceKind,
        Guid resourceId,
        string changeKind,
        TResponse response,
        long streamVersion)
    {
        var correlationId = _context.CorrelationId == Guid.Empty ? Guid.NewGuid() : _context.CorrelationId;
        return new RealtimeResourceChangedV1(
            Guid.CreateVersion7(),
            _context.AccountId,
            _context.WorkspaceId,
            _context.UserId,
            correlationId,
            _context.CausationId,
            _time.UtcNow,
            topicNamespace,
            resourceKind,
            resourceId,
            $"{topicNamespace}:{resourceKind}:{resourceId:N}",
            RealtimeStreamClock.Next(_time.UtcNow),
            changeKind,
            $"{typeof(TResponse).FullName}.v1",
            JsonSerializer.SerializeToElement(response, JsonOptions));
    }
}

/// <summary>Process-wide stream version clock shared by every realtime producer.</summary>
public static class RealtimeStreamClock
{
    private static long _lastClockVersion;

    /// <summary>
    /// The outbox orders a stream by a unique, strictly increasing version. A producer's own resource
    /// version repeats (a semantic no-op keeps it, items created on a board stream all report 1), so
    /// the stream version is the clock: increasing within the process, ordered by time across
    /// processes. The producer's own version is not carried on the stream.
    /// </summary>
    public static long Next(DateTimeOffset now)
    {
        var micros = now.ToUnixTimeMilliseconds() * 1000;
        while (true)
        {
            var last = Interlocked.Read(ref _lastClockVersion);
            var next = Math.Max(last + 1, micros);
            if (Interlocked.CompareExchange(ref _lastClockVersion, next, last) == last)
                return next;
        }
    }
}
