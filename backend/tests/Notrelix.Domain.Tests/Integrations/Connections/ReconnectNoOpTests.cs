using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class ReconnectNoOpTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Reconnect_WhenAlreadyActiveWithSameValues_ShouldBeNoOp()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now,
            providerAccountId: "provider-1", expiresAt: Now.AddDays(30));
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Reconnect("provider-1", Now.AddDays(30), Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reconnect_WhenRevoked_ShouldReauthorize()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now,
            providerAccountId: "provider-1");
        connection.Disconnect(Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Reconnect("provider-1", null, Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionReauthorizedDomainEvent);
    }

    [Fact]
    public void Reconnect_WhenActiveWithDifferentProviderAccountId_ShouldReauthorize()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now,
            providerAccountId: "old-id");
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Reconnect("new-id", null, Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.ProviderAccountId.Should().Be("new-id");
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionReauthorizedDomainEvent);
    }

    [Fact]
    public void Reconnect_WhenActiveWithDifferentExpiresAt_ShouldReauthorize()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now,
            providerAccountId: "p1", expiresAt: Now.AddDays(10));
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Reconnect("p1", Now.AddDays(20), Actor, Now);

        connection.ExpiresAt.Should().Be(Now.AddDays(20));
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionReauthorizedDomainEvent);
    }

    [Fact]
    public void Reconnect_ShouldClearErrorDetail()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkError("some error", Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Reconnect("p1", null, Actor, Now);

        connection.ErrorDetail.Should().BeNull();
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
    }

    [Fact]
    public void Reconnect_ShouldNormalizeProviderAccountId()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Reconnect("  provider-1  ", null, Actor, Now);

        connection.ProviderAccountId.Should().Be("provider-1");
    }

    [Fact]
    public void Reconnect_ShouldIncrementVersion()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var initialVersion = connection.Version;

        connection.Disconnect(Actor, Now);
        connection.Reconnect("p1", null, Actor, Now);

        connection.Version.Should().Be(initialVersion + 2);
    }

    [Fact]
    public void Reconnect_ShouldSetAuditFields()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        connection.Disconnect(Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        var updatedAt = Now.AddMinutes(5);
        var newActor = Guid.NewGuid();
        connection.Reconnect("p1", null, newActor, updatedAt);

        connection.UpdatedBy.Should().Be(newActor);
        connection.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Reconnect_WhenActiveWithSameValuesButErrorDetail_ShouldNotBeNoOp()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now,
            providerAccountId: "provider-1", expiresAt: Now.AddDays(30));
        connection.MarkError("some error", Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Reconnect("provider-1", Now.AddDays(30), Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.ErrorDetail.Should().BeNull();
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionReauthorizedDomainEvent);
    }

    [Fact]
    public void Reconnect_PastExpiration_ShouldThrow()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);

        var act = () => connection.Reconnect("p1", Now.AddDays(-1), Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Reconnect_OnDeletedConnection_ShouldThrow()
    {
        var connection = IntegrationConnection.Create(
            AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        connection.SoftDelete(Actor, Now);

        var act = () => connection.Reconnect("p1", null, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }
}
