using FluentAssertions;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationExecutionVersionTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(AutomationExecution), "SetPayload(System.String)", MutationScenario.Version)]
    [Fact]
    public void SetPayload_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        ((IHasDomainEvents)execution).ClearDomainEvents();
        var version = execution.Version;

        execution.SetPayload("{\"key\":\"value\"}");

        execution.Version.Should().Be(version + 1);
    }

    [CoversMutation(typeof(AutomationExecution), "Start(System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Start_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        ((IHasDomainEvents)execution).ClearDomainEvents();
        var version = execution.Version;

        execution.Start(_now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionStartedDomainEvent);
    }

    [CoversMutation(typeof(AutomationExecution), "Succeed(System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Succeed_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.Start(_now);
        ((IHasDomainEvents)execution).ClearDomainEvents();
        var version = execution.Version;

        execution.Succeed(_now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionSucceededDomainEvent);
    }

    [CoversMutation(typeof(AutomationExecution), "Fail(System.String,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Fail_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.Start(_now);
        ((IHasDomainEvents)execution).ClearDomainEvents();
        var version = execution.Version;

        execution.Fail("error", _now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionFailedDomainEvent);
    }

    [CoversMutation(typeof(AutomationExecution), "Cancel(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Cancel_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), _workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        ((IHasDomainEvents)execution).ClearDomainEvents();
        var version = execution.Version;

        execution.Cancel(_actorId, _now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionCancelledDomainEvent);
    }
}
