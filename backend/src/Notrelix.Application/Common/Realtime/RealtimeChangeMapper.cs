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
            streamVersion,
            changeKind,
            $"{typeof(TResponse).FullName}.v1",
            JsonSerializer.SerializeToElement(response, JsonOptions));
    }
}
