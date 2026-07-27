using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class ErrorEventTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void MarkError_ShouldRaiseErrorRecordedEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.MarkError("Rate limited", Actor, Now);

        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionErrorRecordedDomainEvent);
        var @event = connection.DomainEvents.OfType<IntegrationConnectionErrorRecordedDomainEvent>().Single();
        @event.ErrorDetail.Should().Be("Rate limited");
        @event.RecordedBy.Should().Be(Actor);
    }

    [Fact]
    public void MarkError_ShouldSetStatusToError()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.MarkError("timeout", Actor, Now);

        connection.Status.Should().Be(IntegrationConnectionStatus.Error);
        connection.ErrorDetail.Should().Be("timeout");
    }

    [Fact]
    public void MarkError_WhenSameError_ShouldBeNoOp()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkError("timeout", Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.MarkError("timeout", Actor, Now);

        connection.DomainEvents.Should().BeEmpty();
        connection.Version.Should().Be(2);
    }

    [Fact]
    public void MarkError_WhenDifferentError_ShouldRaiseEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.MarkError("timeout", Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.MarkError("rate limit", Actor, Now);

        connection.ErrorDetail.Should().Be("rate limit");
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionErrorRecordedDomainEvent);
    }

    [Fact]
    public void MarkError_ShouldTrimWhitespace()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        connection.MarkError("  timeout  ", Actor, Now);

        connection.ErrorDetail.Should().Be("timeout");
    }

    [Fact]
    public void MarkError_EmptyError_ShouldThrow()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        var act = () => connection.MarkError("", Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkError_WhenDeleted_ShouldThrow()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        connection.SoftDelete(Actor, Now);

        var act = () => connection.MarkError("error", Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkError_ShouldIncrementVersion()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);
        var initialVersion = connection.Version;

        connection.MarkError("fail", Actor, Now);

        connection.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void MarkError_ShouldUpdateAuditFields()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

        var errorAt = Now.AddMinutes(10);
        var errorActor = Guid.NewGuid();
        connection.MarkError("fail", errorActor, errorAt);

        connection.UpdatedBy.Should().Be(errorActor);
        connection.UpdatedAt.Should().Be(errorAt);
    }
}
