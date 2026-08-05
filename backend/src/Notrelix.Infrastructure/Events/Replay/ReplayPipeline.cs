using Notrelix.Infrastructure.Messaging.Bus;
using Notrelix.Infrastructure.Serialization;

namespace Notrelix.Infrastructure.Events.Replay;

public sealed class ReplayPipeline : IReplayPipeline
{
    private readonly IIntegrationBus _bus;
    private readonly IEventSerializer _serializer;
    private readonly IContractRegistry _contractRegistry;
    private readonly ILogger<ReplayPipeline> _logger;

    public ReplayPipeline(
        IIntegrationBus bus,
        IEventSerializer serializer,
        IContractRegistry contractRegistry,
        ILogger<ReplayPipeline> logger)
    {
        _bus = bus;
        _serializer = serializer;
        _contractRegistry = contractRegistry;
        _logger = logger;
    }

    public async Task<ReplayResult> ExecuteAsync(ReplayRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Authorized)
        {
            return new ReplayResult
            {
                Errors = ["Replay not authorized."],
            };
        }

        var contract = _contractRegistry.Get(request.EventName, request.EventVersion);

        var envelope = new EventEnvelope
        {
            Id = Guid.CreateVersion7(),
            EventName = request.EventName,
            EventVersion = request.EventVersion,
            SourceContext = request.SourceContextOverride ?? "system.replay",
            OccurredAt = request.RequestedAt,
            CorrelationId = request.CorrelationId ?? Guid.CreateVersion7().ToString(),
            TraceParent = request.TraceParentOverride,
            Classification = contract.Classification,
            Data = _serializer.Serialize(new ReplayPayload
            {
                OriginalEventName = request.EventName,
                OriginalEventVersion = request.EventVersion,
                ReplayCorrelationId = request.CorrelationId ?? Guid.CreateVersion7().ToString(),
            }),
        };

        await _bus.PublishAsync(envelope, cancellationToken);

        _logger.LogInformation(
            "Replay pipeline executed for {EventName} v{EventVersion} requested by {RequestedBy}",
            request.EventName, request.EventVersion, request.RequestedBy);

        return new ReplayResult
        {
            Success = true,
            EventsReplayed = 1,
            ReplayCorrelationId = envelope.CorrelationId,
        };
    }

    private sealed record ReplayPayload
    {
        public string OriginalEventName { get; init; } = string.Empty;
        public int OriginalEventVersion { get; init; }
        public string ReplayCorrelationId { get; init; } = string.Empty;
    }
}
