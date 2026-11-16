using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Calls.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>Maps the call fact to the chat.call.changed integration event (outbox, same transaction as the call change).</summary>
public sealed class ChatCallEventMapper : IntegrationEventMapperBase<CallChangedDomainEvent, ChatCallChangedIntegrationEvent>
{
    public override ChatCallChangedIntegrationEvent? Map(CallChangedDomainEvent domainEvent)
    {
        return new ChatCallChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            CallId: domainEvent.CallId,
            InitiatorId: domainEvent.InitiatorId,
            State: domainEvent.State.ToWire(),
            Kind: domainEvent.Kind.ToWire(),
            EndReason: domainEvent.EndReason?.ToWire(),
            RingsMembers: domainEvent.RingsMembers,
            Version: domainEvent.Version,
            ParticipantIds: domainEvent.ParticipantIds,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }
}

/// <summary>Maps the revocation fact to the chat.call.participant-revoked integration event (outbox, same transaction as the release).</summary>
public sealed class ChatCallParticipantRevokedEventMapper : IntegrationEventMapperBase<CallParticipantRevokedDomainEvent, ChatCallParticipantRevokedIntegrationEvent>
{
    public override ChatCallParticipantRevokedIntegrationEvent? Map(CallParticipantRevokedDomainEvent domainEvent) => new(
        EventId: Guid.CreateVersion7(),
        AccountId: domainEvent.AccountId,
        WorkspaceId: domainEvent.WorkspaceId,
        SpaceId: domainEvent.SpaceId,
        CallId: domainEvent.CallId,
        PersonId: domainEvent.PersonId,
        RoomKey: domainEvent.RoomKey,
        CorrelationId: domainEvent.EventId,
        ActorUserId: domainEvent.PersonId,
        CausationId: null,
        OccurredAt: domainEvent.OccurredAt);
}
