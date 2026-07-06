using FluentAssertions;
using Notrelix.Domain.Automation.Executions;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationExecutionTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Queued);
        execution.AttemptCount.Should().Be(0);
        execution.DomainEvents.Should().ContainSingle(e => e is AutomationExecutionQueuedDomainEvent);
    }

    [Fact]
    public void SetPayload_ShouldSetPayload()
    {
        var execution = CreateExecution();

        execution.SetPayload("{\"data\":\"test\"}");

        execution.Payload.Should().Be("{\"data\":\"test\"}");
    }

    [Fact]
    public void Start_ShouldTransition_AndRaiseEvent()
    {
        var execution = CreateExecution();
        execution.ClearDomainEvents();

        execution.Start(DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Running);
        execution.DomainEvents.Should().ContainSingle(e => e is AutomationExecutionStartedDomainEvent);
    }

    [Fact]
    public void Start_WhenNotQueued_ShouldThrow()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);

        var act = () => execution.Start(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Queued*");
    }

    [Fact]
    public void Succeed_ShouldTransition_AndRaiseEvent()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);
        execution.ClearDomainEvents();

        execution.Succeed(DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Succeeded);
        execution.DomainEvents.Should().ContainSingle(e => e is AutomationExecutionSucceededDomainEvent);
    }

    [Fact]
    public void Succeed_WhenNotRunning_ShouldThrow()
    {
        var execution = CreateExecution();

        var act = () => execution.Succeed(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Running*");
    }

    [Fact]
    public void Fail_ShouldTransition_AndRaiseEvent()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);
        execution.ClearDomainEvents();

        execution.Fail("Timeout", DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Failed);
        execution.Error.Should().Be("Timeout");
        execution.DomainEvents.Should().ContainSingle(e => e is AutomationExecutionFailedDomainEvent);
    }

    [Fact]
    public void Fail_WhenNotRunning_ShouldThrow()
    {
        var execution = CreateExecution();

        var act = () => execution.Fail("Error", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Running*");
    }

    [Fact]
    public void Fail_WithEmptyError_ShouldThrow()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);

        var act = () => execution.Fail("", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*empty*");
    }

    [Fact]
    public void Cancel_FromQueued_ShouldTransition()
    {
        var execution = CreateExecution();

        execution.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Cancelled);
        execution.DomainEvents.Should().ContainSingle(e => e is AutomationExecutionCancelledDomainEvent);
    }

    [Fact]
    public void Cancel_FromRunning_ShouldTransition()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);
        execution.ClearDomainEvents();

        execution.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadySucceeded_ShouldThrow()
    {
        var execution = CreateExecution();
        execution.Start(DateTimeOffset.UtcNow);
        execution.Succeed(DateTimeOffset.UtcNow);

        var act = () => execution.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ExecutionStep_ShouldCreateWithQueuedStatus()
    {
        var step = AutomationExecutionStep.Create(Guid.NewGuid(), Guid.NewGuid());

        step.Status.Should().Be(AutomationExecutionStatus.Queued);
    }

    [Fact]
    public void ExecutionStep_Start_ShouldTransition()
    {
        var step = AutomationExecutionStep.Create(Guid.NewGuid(), Guid.NewGuid());

        step.Start(DateTimeOffset.UtcNow);

        step.Status.Should().Be(AutomationExecutionStatus.Running);
    }

    [Fact]
    public void ExecutionStep_Succeed_ShouldTransition()
    {
        var step = AutomationExecutionStep.Create(Guid.NewGuid(), Guid.NewGuid());
        step.Start(DateTimeOffset.UtcNow);

        step.Succeed(DateTimeOffset.UtcNow);

        step.Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    [Fact]
    public void ExecutionStep_Fail_ShouldTransition()
    {
        var step = AutomationExecutionStep.Create(Guid.NewGuid(), Guid.NewGuid());
        step.Start(DateTimeOffset.UtcNow);

        step.Fail("Error", DateTimeOffset.UtcNow);

        step.Status.Should().Be(AutomationExecutionStatus.Failed);
        step.Error.Should().Be("Error");
    }

    [Fact]
    public void ExecutionStep_Start_WhenNotQueued_ShouldThrow()
    {
        var step = AutomationExecutionStep.Create(Guid.NewGuid(), Guid.NewGuid());
        step.Start(DateTimeOffset.UtcNow);

        var act = () => step.Start(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Queued*");
    }

    [Fact]
    public void ExecutionStep_Succeed_WhenNotRunning_ShouldThrow()
    {
        var step = AutomationExecutionStep.Create(Guid.NewGuid(), Guid.NewGuid());

        var act = () => step.Succeed(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Running*");
    }

    [Fact]
    public void ExecutionStep_Fail_WhenNotRunning_ShouldThrow()
    {
        var step = AutomationExecutionStep.Create(Guid.NewGuid(), Guid.NewGuid());

        var act = () => step.Fail("Error", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Running*");
    }

    private static AutomationExecution CreateExecution()
    {
        return AutomationExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
