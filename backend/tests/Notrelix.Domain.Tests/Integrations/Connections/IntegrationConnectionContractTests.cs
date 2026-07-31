using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class IntegrationConnectionContractTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.MarkError), MutationScenario.Valid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MarkError_ShouldStoreErrorDetail()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.MarkError("Rate limit exceeded", Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Error);
        connection.ErrorDetail.Should().Be("Rate limit exceeded");
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.MarkError), MutationScenario.NoOp, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MarkError_WhenAlreadyError_ShouldUpdateErrorDetail()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkError("First error", Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.MarkError("Second error", Actor, Now);

        connection.ErrorDetail.Should().Be("Second error");
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.MarkError), MutationScenario.Invalid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MarkError_EmptyError_ShouldThrow()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        var act = () => connection.MarkError("", Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.AddScope), MutationScenario.Scope, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void AddScope_TrimmedBeforeComparison()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.AddScope("read", Actor, Now);
        connection.AddScope(" read ", Actor, Now);

        connection.Scopes.Should().ContainSingle(s => s.Scope == "read");
    }

    [Fact]
    public void RemoveScope_TrimmedBeforeComparison()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.AddScope("read", Actor, Now);

        connection.RemoveScope(" read ", Actor, Now);

        connection.Scopes.Should().BeEmpty();
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldSetAudit()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        var deletedBy = Guid.NewGuid();
        var deletedAt = DateTimeOffset.UtcNow;
        connection.Delete(deletedBy, deletedAt);

        connection.IsDeleted.Should().BeTrue();
        connection.UpdatedAt.Should().Be(deletedAt);
        connection.UpdatedBy.Should().Be(deletedBy);
        connection.Version.Should().Be(2);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionDeletedDomainEvent);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldSetAudit()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        var restoredBy = Guid.NewGuid();
        var restoredAt = DateTimeOffset.UtcNow;
        connection.Restore(restoredBy, restoredAt);

        connection.IsDeleted.Should().BeFalse();
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.UpdatedAt.Should().Be(restoredAt);
        connection.UpdatedBy.Should().Be(restoredBy);
        connection.Version.Should().Be(3);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionRestoredDomainEvent);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Delete), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.Delete(Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Delete(Actor, Now);

        connection.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Restore), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Restore(Actor, Now);

        connection.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Disconnect_ShouldRaiseRevokedEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Disconnect(Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Revoked);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionRevokedDomainEvent);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Reconnect), MutationScenario.Event, typeof(string), typeof(DateTimeOffset?), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reconnect_ShouldRaiseReauthorizedEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.Disconnect(Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Reconnect("provider-1", Now.AddDays(1), Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionReauthorizedDomainEvent);
    }

    [Fact]
    public void Lifecycle_VersionTrack_ShouldIncrementCorrectly()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.Version.Should().Be(1);

        connection.Disconnect(Actor, Now);
        connection.Version.Should().Be(2);

        connection.Reconnect("p1", Now.AddDays(1), Actor, Now);
        connection.Version.Should().Be(3);

        connection.MarkExpired(Actor, Now);
        connection.Version.Should().Be(4);

        connection.MarkError("fail", Actor, Now);
        connection.Version.Should().Be(5);
    }
}
