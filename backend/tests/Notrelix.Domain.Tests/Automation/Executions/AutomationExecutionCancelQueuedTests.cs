using FluentAssertions;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.Executions;

namespace Notrelix.Domain.Tests.Automation;

/// <summary>
/// UC-AUT-H04: a person cancels a Queued execution. A running or finished execution is refused.
/// </summary>
public class AutomationExecutionCancelQueuedTests
{
    private static AutomationExecution CreateExecution() =>
        AutomationExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void CancelBeforeStart_FromQueued_EndsCancelled_AndRaisesCancelledEvent()
    {
        var execution = CreateExecution();
        ((IHasDomainEvents)execution).ClearDomainEvents();
        var at = DateTimeOffset.UtcNow;

        execution.CancelBeforeStart(Guid.NewGuid(), at);

        execution.Status.Should().Be(AutomationExecutionStatus.Cancelled);
        execution.FinishedAt.Should().Be(at);
        execution.DomainEvents.Should().ContainSingle(e => e is AutomationExecutionCancelledDomainEvent);
    }

    [Fact]
    public void CancelBeforeStart_FromRunning_IsRefused()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);

        var act = () => execution.CancelBeforeStart(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Execution_CannotCancelUnlessQueued);
        execution.Status.Should().Be(AutomationExecutionStatus.Running);
    }

    [Fact]
    public void CancelBeforeStart_FromSucceeded_IsRefused()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);
        execution.Succeed(DateTimeOffset.UtcNow);

        var act = () => execution.CancelBeforeStart(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        execution.Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    [Fact]
    public void CancelBeforeStart_FromFailed_IsRefused()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);
        execution.Fail(AutomationFailureClass.TargetRejected, "n8n reported that the workflow failed.", DateTimeOffset.UtcNow);

        var act = () => execution.CancelBeforeStart(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        execution.Status.Should().Be(AutomationExecutionStatus.Failed);
    }

    [Fact]
    public void CancelBeforeStart_TwiceSecondIsRefused()
    {
        var execution = CreateExecution();
        execution.CancelBeforeStart(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => execution.CancelBeforeStart(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void CancelBeforeStart_RefusedCall_LeavesVersionUnchanged()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);
        var versionBefore = execution.Version;

        var act = () => execution.CancelBeforeStart(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        execution.Version.Should().Be(versionBefore);
    }
}
