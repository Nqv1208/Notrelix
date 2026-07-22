using Notrelix.Application.Common.Events;
using Notrelix.Platform.Messaging.Contracts;

namespace Notrelix.Platform.Messaging.Runtime;

public sealed class EnvelopeBuilder
{
    private readonly IEventDescriptorProvider _descriptorProvider;
    private readonly IEventSerializer _serializer;

    public EnvelopeBuilder(
        IEventDescriptorProvider descriptorProvider,
        IEventSerializer serializer)
    {
        _descriptorProvider = descriptorProvider;
        _serializer = serializer;
    }

    public EventEnvelope Build(EventPublication publication)
    {
        var eventType = publication.Event.GetType();
        var descriptor = _descriptorProvider.Get(eventType);

        var data = _serializer.Serialize(publication.Event, publication.Event.GetType());

        return new EventEnvelope
        {
            Id = Guid.NewGuid(),
            EventName = descriptor.Name,
            EventVersion = descriptor.Version,
            SourceContext = publication.Context.SourceContext,
            AggregateType = publication.Context.AggregateType,
            AggregateId = publication.Context.AggregateId,
            WorkspaceId = publication.Context.WorkspaceId,
            ActorUserId = publication.Context.ActorUserId,
            CorrelationId = publication.Context.CorrelationId,
            CausationId = publication.Context.CausationId,
            TraceParent = publication.Context.TraceParent,
            TraceState = publication.Context.TraceState,
            OccurredAt = publication.Context.OccurredAt,
            Data = data,
            ContentType = "application/json",
            Classification = descriptor.Classification,
            AccountId = publication.Context.AccountId,
        };
    }
}
