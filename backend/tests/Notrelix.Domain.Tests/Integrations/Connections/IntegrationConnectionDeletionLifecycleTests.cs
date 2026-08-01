using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class IntegrationConnectionDeletionLifecycleTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Active_SurvivesDeleteRestore()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.Delete(Actor, Now);
        connection.Restore(Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Error_SurvivesDeleteRestore_RetainsErrorDetail()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkError("connection lost", Actor, Now);

        connection.Delete(Actor, Now);
        connection.Restore(Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Error);
        connection.ErrorDetail.Should().Be("connection lost");
        connection.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Expired_SurvivesDeleteRestore()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkExpired(Actor, Now);

        connection.Delete(Actor, Now);
        connection.Restore(Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Expired);
        connection.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Revoked_SurvivesDeleteRestore()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.Disconnect(Actor, Now);

        connection.Delete(Actor, Now);
        connection.Restore(Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Revoked);
        connection.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Delete_DoesNotRaiseRevokedEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Delete(Actor, Now);

        connection.DomainEvents.Should().NotContain(e => e is IntegrationConnectionRevokedDomainEvent);
    }

    [Fact]
    public void Restore_DoesNotRaiseReauthorizedEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.Delete(Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.Restore(Actor, Now);

        connection.DomainEvents.Should().NotContain(e => e is IntegrationConnectionReauthorizedDomainEvent);
    }

    [Fact]
    public void DeleteReason_IsNullWhenOmitted()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.Delete(Actor, Now);

        connection.DeleteReason.Should().BeNull();
    }
}
