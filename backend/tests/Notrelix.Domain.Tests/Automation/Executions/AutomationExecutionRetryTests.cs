using FluentAssertions;
using Notrelix.Domain.Automation.Executions;

namespace Notrelix.Domain.Tests.Automation;

/// <summary>
/// TAC-AI-003/004 — the Automation-owned retry semantic: a retryable dispatch
/// failure records attempt evidence and returns the execution to Queued for
/// another Automation attempt under the same execution identity. Terminal
/// business/provider rejection stays on the normal Fail transition.
/// </summary>
public class AutomationExecutionRetryTests
{
    private static AutomationExecution CreateRunningExecution()
    {
        var execution = AutomationExecution.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        execution.Start(DateTimeOffset.UtcNow);
        return execution;
    }

    [Fact]
    public void RecordRetryableDispatchFailure_FromRunning_RequeuesAndIncrementsAttempt()
    {
        var execution = CreateRunningExecution();
        ((IHasDomainEvents)execution).ClearDomainEvents();

        execution.RecordRetryableDispatchFailure("n8n unreachable", DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Queued,
            "the execution returns to Queued for another Automation attempt");
        execution.AttemptCount.Should().Be(1);
        execution.Error.Should().Be("n8n unreachable");
        execution.FinishedAt.Should().BeNull("a retryable failure is not a terminal outcome");
    }

    [Fact]
    public void RecordRetryableDispatchFailure_Twice_IncrementsAttemptCountMonotonically()
    {
        var execution = CreateRunningExecution();

        execution.RecordRetryableDispatchFailure("first attempt failed", DateTimeOffset.UtcNow);
        execution.Start(DateTimeOffset.UtcNow);
        execution.RecordRetryableDispatchFailure("second attempt failed", DateTimeOffset.UtcNow);

        execution.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void RecordRetryableDispatchFailure_FromQueued_Throws()
    {
        var execution = AutomationExecution.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => execution.RecordRetryableDispatchFailure("error", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*running*");
    }

    [Fact]
    public void RecordRetryableDispatchFailure_WithEmptyError_Throws()
    {
        var execution = CreateRunningExecution();

        var act = () => execution.RecordRetryableDispatchFailure(" ", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*empty*");
    }

    [Fact]
    public void TerminalFailure_AfterRunning_StaysFailed()
    {
        var execution = CreateRunningExecution();

        execution.Fail("rule configuration invalid", DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Failed);
        execution.AttemptCount.Should().Be(0,
            "terminal business rejection is not a retry attempt");
    }
}
