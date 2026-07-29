using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations;

[CoversAggregate(typeof(IntegrationConnection))]
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

    [CoversMutation(typeof(IntegrationConnection), "Reconnect(System.String,System.DateTimeOffset?,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
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

    [CoversMutation(typeof(IntegrationConnection), "Reconnect(System.String,System.DateTimeOffset?,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Reconnect_WhenAlreadyActiveWithSameValues_ShouldBeNoOp()
    {
        var expiresAt = Now.AddDays(1);
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now, "provider-acc-1", expiresAt);
        ((IHasDomainEvents)connection).ClearDomainEvents();
        var versionBefore = connection.Version;

        connection.Reconnect("provider-acc-1", expiresAt, Actor, Now);

        connection.Version.Should().Be(versionBefore);
        connection.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(IntegrationConnection), "Reconnect(System.String,System.DateTimeOffset?,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Reconnect_ShouldNormalizeProviderAccountId()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.Reconnect("  provider-acc-1  ", Now.AddDays(1), Actor, Now);

        connection.ProviderAccountId.Should().Be("provider-acc-1");
    }

    [CoversMutation(typeof(IntegrationConnection), "Reconnect(System.String,System.DateTimeOffset?,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Reconnect_ShouldClearErrorDetail()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkError("Some error", Actor, Now);

        connection.Reconnect("provider-acc-1", Now.AddDays(1), Actor, Now);

        connection.ErrorDetail.Should().BeNull();
    }

    [CoversMutation(typeof(IntegrationConnection), "MarkExpired(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void MarkExpired_ShouldSetExpiredStatus()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkExpired(Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Expired);
    }

    [CoversMutation(typeof(IntegrationConnection), "MarkError(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(IntegrationConnection), "MarkExpired(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MarkError_ShouldSetErrorStatus_AndRaiseEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.MarkError("Connection failed", Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Error);
        connection.ErrorDetail.Should().Be("Connection failed");
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionErrorRecordedDomainEvent);
    }

    [CoversMutation(typeof(IntegrationConnection), "MarkError(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(IntegrationConnection), "MarkExpired(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void MarkError_WhenSameErrorAlreadySet_ShouldBeNoOp()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkError("Connection failed", Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();
        var versionBefore = connection.Version;

        connection.MarkError("Connection failed", Actor, Now);

        connection.Version.Should().Be(versionBefore);
        connection.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(IntegrationConnection), "MarkError(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(IntegrationConnection), "MarkExpired(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MarkError_ShouldTrimErrorDetail()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.MarkError("  Connection failed  ", Actor, Now);

        connection.ErrorDetail.Should().Be("Connection failed");
    }

    [CoversMutation(typeof(IntegrationConnection), "RotateSecret(System.String,Notrelix.Domain.SharedKernel.SecretRef,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void RotateSecret_ShouldUpdateCurrentSecret_AndRaiseEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        var secretRef1 = SecretRef.Create("secret-key-1");
        var secretRef2 = SecretRef.Create("secret-key-2");

        connection.RotateSecret("v1", secretRef1, Actor, Now);
        connection.CurrentSecretVersion.Should().Be("v1");
        connection.CurrentSecretRef!.Value.Should().Be("secret-key-1");
        connection.DomainEvents.Should().Contain(e => e is IntegrationSecretRotatedDomainEvent);

        // Rotating to a new version updates the current secret
        ((IHasDomainEvents)connection).ClearDomainEvents();
        connection.RotateSecret("v2", secretRef2, Actor, Now);
        connection.CurrentSecretVersion.Should().Be("v2");
        connection.CurrentSecretRef!.Value.Should().Be("secret-key-2");
        connection.DomainEvents.Should().Contain(e => e is IntegrationSecretRotatedDomainEvent);
    }

    [CoversMutation(typeof(IntegrationConnection), "RotateSecret(System.String,Notrelix.Domain.SharedKernel.SecretRef,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void RotateSecret_WhenSameVersionAndSecret_ShouldBeNoOp()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        var secretRef = SecretRef.Create("secret-key-1");

        connection.RotateSecret("v1", secretRef, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();
        var versionBefore = connection.Version;

        connection.RotateSecret("v1", secretRef, Actor, Now);

        connection.Version.Should().Be(versionBefore);
        connection.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(IntegrationConnection), "RemoveScope(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Scope)]
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
