using Notrelix.Application.Common.Messaging;
using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Members.Events;
using Notrelix.Application.EventMappers.Workspaces;
using Notrelix.Application.Events.Workspaces;

namespace Notrelix.Application.Tests.EventMappers.Workspaces;

/// <summary>
/// TAC-WG-006/007 — the pinned Workspaces membership integration event is
/// producer-owned, registry-registered, and mapped from the membership Domain
/// fact. Proves producer ownership and mapping without re-teaching delivery
/// mechanics (dedup/retry are owned by Platform tests).
/// </summary>
public class WorkspaceMembershipEventReferenceTests
{
    private static WorkspaceMemberAddedDomainEvent CreateMemberAddedDomainEvent()
        => new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Domain.Workspaces.Members.WorkspaceRole.Member,
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow);

    [Fact]
    public void WorkspaceMemberAddedEvent_IsProducerOwnedByWorkspaces()
    {
        typeof(WorkspaceMemberAddedIntegrationEvent).Namespace.Should().Be(
            "Notrelix.Application.Events.Workspaces",
            "membership outward facts belong to the Workspaces producer");
    }

    [Fact]
    public void WorkspaceMemberAddedEvent_HasStableRegistryIdentity()
    {
        var attribute = typeof(WorkspaceMemberAddedIntegrationEvent)
            .GetCustomAttributes(typeof(EventNameAttribute), inherit: false)
            .OfType<EventNameAttribute>()
            .SingleOrDefault();

        attribute.Should().NotBeNull("the outward contract must register a stable identity");
        attribute!.Name.Should().Be("workspace.member.added");
        attribute.Version.Should().Be(1);
    }

    [Fact]
    public void WorkspaceEventMapper_MapsMemberAddedDomainFact_ToOutwardEvent()
    {
        var mapper = new WorkspaceEventMapper();
        var domainEvent = CreateMemberAddedDomainEvent();

        var integrationEvent = mapper.Map(domainEvent);

        integrationEvent.Should().NotBeNull();
        integrationEvent!.WorkspaceId.Should().Be(domainEvent.WorkspaceId);
        integrationEvent.UserId.Should().Be(domainEvent.UserId);
        integrationEvent.Role.Should().Be(domainEvent.Role.ToString());
        integrationEvent.CorrelationId.Should().Be(domainEvent.EventId);
        integrationEvent.ActorUserId.Should().Be(domainEvent.ActorId);
    }

    [Fact]
    public void WorkspaceMemberAddedEvent_CarriesEnvelopeIdentity()
    {
        var eventId = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7();
        var integrationEvent = new WorkspaceMemberAddedIntegrationEvent(
            EventId: eventId,
            WorkspaceId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            Role: "Member",
            CorrelationId: correlationId,
            ActorUserId: Guid.CreateVersion7(),
            CausationId: null,
            OccurredAt: DateTimeOffset.UtcNow);

        integrationEvent.EventId.Should().Be(eventId);
        integrationEvent.CorrelationId.Should().Be(correlationId);
        integrationEvent.MessageName.Should().Be("workspace.member.added");
        integrationEvent.SchemaVersion.Should().Be(1);
    }
}
