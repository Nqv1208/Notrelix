using FluentAssertions;
using Notrelix.Domain.Automation.Executions;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationExecutionVersionTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void SetPayload_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.SetPayload("{\"key\":\"value\"}");

        execution.Version.Should().Be(version + 1);
    }

    [Fact]
    public void Start_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.Start(_now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionStartedDomainEvent);
    }

    [Fact]
    public void Succeed_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.Start(_now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.Succeed(_now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionSucceededDomainEvent);
    }

    [Fact]
    public void Fail_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.Start(_now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.Fail("error", _now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionFailedDomainEvent);
    }

    [Fact]
    public void Cancel_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.Cancel(_actorId, _now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionCancelledDomainEvent);
    }
}
