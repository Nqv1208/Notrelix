using FluentAssertions;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations;

public class IntegrationConnectionTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.Provider.Should().Be(IntegrationProvider.Slack);
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithInvalidExpiration_ShouldThrowDomainException()
    {
        var expiresAt = Now.AddMinutes(-5);

        var act = () => IntegrationConnection.Create(
            AccountId,
            WorkspaceId,
            IntegrationProvider.Slack,
            Actor,
            Now,
            expiresAt: expiresAt);

        act.Should().Throw<DomainException>().WithMessage("Expiration time must be in the future.");
    }

    [Fact]
    public void Disconnect_ShouldSetRevokedStatus_AndRaiseEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.Disconnect(Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Revoked);
        connection.DomainEvents.Should().Contain(e => e is IntegrationConnectionRevokedDomainEvent);
    }

    [Fact]
    public void Reconnect_ShouldSetActiveStatus_AndValidateExpiration()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.Disconnect(Actor, Now);
        connection.Reconnect("provider-acc-1", Now.AddDays(1), Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.ProviderAccountId.Should().Be("provider-acc-1");
        connection.DomainEvents.Should().Contain(e => e is IntegrationConnectionReauthorizedDomainEvent);

        var act = () => connection.Reconnect("provider-acc-1", Now.AddDays(-1), Actor, Now);
        act.Should().Throw<DomainException>().WithMessage("Expiration time must be in the future.");
    }

    [Fact]
    public void MarkExpired_ShouldSetExpiredStatus()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkExpired(Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Expired);
    }

    [Fact]
    public void RotateSecret_ShouldAddVersion_AndNotAllowDuplicates()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        var secretRef1 = SecretRef.Create("secret-key-1");
        var secretRef2 = SecretRef.Create("secret-key-2");

        connection.RotateSecret("v1", secretRef1, Actor, Now);
        connection.SecretVersions.Should().ContainSingle(v => v.Version == "v1" && v.SecretReference.Value == "secret-key-1");
        connection.DomainEvents.Should().Contain(e => e is IntegrationSecretRotatedDomainEvent);

        var act = () => connection.RotateSecret("v1", secretRef2, Actor, Now);
        act.Should().Throw<DomainException>().WithMessage("Secret version 'v1' already exists for this connection.");
    }

    [Fact]
    public void AddAndRemoveScope_ShouldManageScopes_AndRaiseEvents()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.AddScope("read", Actor, Now);
        connection.Scopes.Should().ContainSingle(s => s.Scope == "read");
        connection.DomainEvents.Should().Contain(e => e is IntegrationScopeAddedDomainEvent);

        // duplicate add ignored
        connection.AddScope("read", Actor, Now);
        connection.Scopes.Should().HaveCount(1);

        connection.RemoveScope("read", Actor, Now);
        connection.Scopes.Should().BeEmpty();
        connection.DomainEvents.Should().Contain(e => e is IntegrationScopeRemovedDomainEvent);
    }

    [Fact]
    public void CalendarIntegration_Lifecycle_ShouldWork()
    {
        var connectionId = Guid.NewGuid();

        var calendar = CalendarIntegration.Create(AccountId, WorkspaceId, connectionId, CalendarProvider.Google, CalendarSyncDirection.Both, Actor, Now);
        calendar.IsActive.Should().BeTrue();
        calendar.SyncDirection.Should().Be(CalendarSyncDirection.Both);
        calendar.DomainEvents.Should().Contain(e => e is CalendarIntegrationConnectedDomainEvent);

        calendar.Deactivate(Actor, Now);
        calendar.IsActive.Should().BeFalse();
        calendar.DomainEvents.Should().Contain(e => e is CalendarIntegrationDeactivatedDomainEvent);

        calendar.Activate(Actor, Now);
        calendar.IsActive.Should().BeTrue();
        calendar.DomainEvents.Should().Contain(e => e is CalendarIntegrationActivatedDomainEvent);

        calendar.ChangeSyncDirection(CalendarSyncDirection.Push, Actor, Now);
        calendar.SyncDirection.Should().Be(CalendarSyncDirection.Push);
        calendar.DomainEvents.Should().Contain(e => e is CalendarIntegrationSyncDirectionChangedDomainEvent);
    }

    [Fact]
    public void CalendarIntegration_LinkEvent_ShouldEnforceUniqueness()
    {
        var connectionId = Guid.NewGuid();
        var calendar = CalendarIntegration.Create(AccountId, WorkspaceId, connectionId, CalendarProvider.Google, CalendarSyncDirection.Both, Actor, Now);

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
