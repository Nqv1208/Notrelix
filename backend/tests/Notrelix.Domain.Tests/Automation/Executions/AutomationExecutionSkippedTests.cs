using FluentAssertions;
using Notrelix.Domain.Automation.Executions;

namespace Notrelix.Domain.Tests.Automation;

/// <summary>
/// UC-AUT-C02: a condition that is not met is recorded as a Skipped execution. It is retained with its finish time,
/// it carries no failure, and it raises no queued, started or failed fact.
/// </summary>
public class AutomationExecutionSkippedTests
{
    [Fact]
    public void CreateSkipped_IsRetainedAsSkipped_WithoutFailureOrEvents()
    {
        var occurredAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var skippedAt = DateTimeOffset.UtcNow;

        var execution = AutomationExecution.CreateSkipped(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), occurredAt, 2, "{}", skippedAt);

        execution.Status.Should().Be(AutomationExecutionStatus.Skipped);
        execution.FinishedAt.Should().Be(skippedAt);
        execution.StartedAt.Should().Be(occurredAt);
        execution.Error.Should().BeNull();
        execution.FailureClass.Should().BeNull();
        execution.AttemptCount.Should().Be(0);
        execution.RuleVersion.Should().Be(2);
        execution.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CreateSkipped_RejectsAnEmptyRuleIdentity()
    {
        var act = () => AutomationExecution.CreateSkipped(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow, 1, null, DateTimeOffset.UtcNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Skipped_IsNotAFailedOrRunningStatus()
    {
        AutomationExecutionStatus.Skipped.Should().NotBe(AutomationExecutionStatus.Failed);
        AutomationExecutionStatus.Skipped.Should().NotBe(AutomationExecutionStatus.Running);
        AutomationExecutionStatus.Skipped.Should().NotBe(AutomationExecutionStatus.Queued);
    }
}
