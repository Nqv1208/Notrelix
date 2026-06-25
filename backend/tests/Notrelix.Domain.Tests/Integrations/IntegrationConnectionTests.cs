using FluentAssertions;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations;

public class IntegrationConnectionTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var connection = IntegrationConnection.Create(workspaceId, IntegrationProvider.Slack, Guid.NewGuid(), DateTimeOffset.UtcNow);

        connection.Provider.Should().Be(IntegrationProvider.Slack);
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithInvalidExpiration_ShouldThrowDomainException()
    {
        var workspaceId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddMinutes(-5);

        var act = () => IntegrationConnection.Create(
            workspaceId,
            IntegrationProvider.Slack,
            Guid.NewGuid(),
            createdAt,
            expiresAt: expiresAt);

        act.Should().Throw<DomainException>().WithMessage("Expiration time must be in the future.");
    }

    [Fact]
    public void Disconnect_ShouldSetRevokedStatus_AndRaiseEvent()
    {
        var connection = IntegrationConnection.Create(Guid.NewGuid(), IntegrationProvider.Slack, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        connection.Disconnect(actor, now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Revoked);
        connection.DomainEvents.Should().Contain(e => e is IntegrationConnectionRevokedDomainEvent);
    }

    [Fact]
    public void Reconnect_ShouldSetActiveStatus_AndValidateExpiration()
    {
        var connection = IntegrationConnection.Create(Guid.NewGuid(), IntegrationProvider.Slack, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        connection.Disconnect(actor, now);
        connection.Reconnect("provider-acc-1", now.AddDays(1), actor, now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.ProviderAccountId.Should().Be("provider-acc-1");
        connection.DomainEvents.Should().Contain(e => e is IntegrationConnectionReauthorizedDomainEvent);

        var act = () => connection.Reconnect("provider-acc-1", now.AddDays(-1), actor, now);
        act.Should().Throw<DomainException>().WithMessage("Expiration time must be in the future.");
    }

    [Fact]
    public void MarkExpired_ShouldSetExpiredStatus()
    {
        var connection = IntegrationConnection.Create(Guid.NewGuid(), IntegrationProvider.Slack, Guid.NewGuid(), DateTimeOffset.UtcNow);
        connection.MarkExpired(Guid.NewGuid(), DateTimeOffset.UtcNow);

        connection.Status.Should().Be(IntegrationConnectionStatus.Expired);
    }

    [Fact]
    public void RotateSecret_ShouldAddVersion_AndNotAllowDuplicates()
    {
        var connection = IntegrationConnection.Create(Guid.NewGuid(), IntegrationProvider.Slack, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var secretRef1 = SecretRef.Create("secret-key-1");
        var secretRef2 = SecretRef.Create("secret-key-2");
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        connection.RotateSecret("v1", secretRef1, actor, now);
        connection.SecretVersions.Should().ContainSingle(v => v.Version == "v1" && v.SecretReference.Value == "secret-key-1");
        connection.DomainEvents.Should().Contain(e => e is IntegrationSecretRotatedDomainEvent);

        var act = () => connection.RotateSecret("v1", secretRef2, actor, now);
        act.Should().Throw<DomainException>().WithMessage("Secret version 'v1' already exists for this connection.");
    }

    [Fact]
    public void AddAndRemoveScope_ShouldManageScopes_AndRaiseEvents()
    {
        var connection = IntegrationConnection.Create(Guid.NewGuid(), IntegrationProvider.Slack, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        connection.AddScope("read", actor, now);
        connection.Scopes.Should().ContainSingle(s => s.Scope == "read");
        connection.DomainEvents.Should().Contain(e => e is IntegrationScopeAddedDomainEvent);

        // duplicate add ignored
        connection.AddScope("read", actor, now);
        connection.Scopes.Should().HaveCount(1);

        connection.RemoveScope("read", actor, now);
        connection.Scopes.Should().BeEmpty();
        connection.DomainEvents.Should().Contain(e => e is IntegrationScopeRemovedDomainEvent);
    }

    [Fact]
    public void CalendarIntegration_Lifecycle_ShouldWork()
    {
        var workspaceId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var calendar = CalendarIntegration.Create(workspaceId, connectionId, CalendarProvider.Google, CalendarSyncDirection.Both, actor, now);
        calendar.IsActive.Should().BeTrue();
        calendar.SyncDirection.Should().Be(CalendarSyncDirection.Both);
        calendar.DomainEvents.Should().Contain(e => e is CalendarIntegrationConnectedDomainEvent);

        calendar.Deactivate(actor, now);
        calendar.IsActive.Should().BeFalse();
        calendar.DomainEvents.Should().Contain(e => e is CalendarIntegrationDeactivatedDomainEvent);

        calendar.Activate(actor, now);
        calendar.IsActive.Should().BeTrue();
        calendar.DomainEvents.Should().Contain(e => e is CalendarIntegrationActivatedDomainEvent);

        calendar.ChangeSyncDirection(CalendarSyncDirection.Push, actor, now);
        calendar.SyncDirection.Should().Be(CalendarSyncDirection.Push);
        calendar.DomainEvents.Should().Contain(e => e is CalendarIntegrationSyncDirectionChangedDomainEvent);
    }

    [Fact]
    public void CalendarIntegration_LinkEvent_ShouldEnforceUniqueness()
    {
        var workspaceId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var calendar = CalendarIntegration.Create(workspaceId, connectionId, CalendarProvider.Google, CalendarSyncDirection.Both, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var internalId = Guid.NewGuid();
        var externalId = "ext-event-123";

        calendar.LinkEvent(internalId, externalId, "etag1");
        calendar.EventLinks.Should().ContainSingle(l => l.InternalEventId == internalId && l.ExternalEventId == externalId && l.ETag == "etag1");

        // Duplicate internal or external id link should throw
        var act1 = () => calendar.LinkEvent(internalId, "ext-event-456");
        act1.Should().Throw<DomainException>();

        var act2 = () => calendar.LinkEvent(Guid.NewGuid(), externalId);
        act2.Should().Throw<DomainException>();

        calendar.UpdateEventLinkETag(internalId, "etag2");
        calendar.EventLinks.First().ETag.Should().Be("etag2");
    }
}
