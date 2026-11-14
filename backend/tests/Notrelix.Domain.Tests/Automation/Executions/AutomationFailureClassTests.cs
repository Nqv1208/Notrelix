using FluentAssertions;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.Executions;

namespace Notrelix.Domain.Tests.Automation;

/// <summary>
/// UC-AUT-I04: the terminal versus retryable classification is a stable contract. Each class has one stable code,
/// only Transient is retryable, a terminal class ends the execution, and a retry is recorded only for Transient.
/// </summary>
public class AutomationFailureClassTests
{
    public static IEnumerable<object[]> ClassCodes() =>
    [
        [AutomationFailureClass.TargetGone, "target_gone"],
        [AutomationFailureClass.PermissionRevoked, "permission_revoked"],
        [AutomationFailureClass.InvalidConfiguration, "invalid_configuration"],
        [AutomationFailureClass.RuleInactive, "rule_inactive"],
        [AutomationFailureClass.TargetRejected, "target_rejected"],
        [AutomationFailureClass.OutcomeUnknown, "outcome_unknown"],
        [AutomationFailureClass.Transient, "transient"],
    ];

    public static IEnumerable<object[]> TerminalClasses() =>
        ClassCodes().Where(row => (AutomationFailureClass)row[0] != AutomationFailureClass.Transient);

    private static AutomationExecution RunningExecution()
    {
        var execution = AutomationExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        execution.Start(DateTimeOffset.UtcNow);
        return execution;
    }

    [Theory]
    [MemberData(nameof(ClassCodes))]
    public void Code_IsTheStableStringOfEachClass(AutomationFailureClass failureClass, string code)
    {
        failureClass.Code().Should().Be(code);
    }

    [Theory]
    [MemberData(nameof(ClassCodes))]
    public void TryParse_ReadsBackEachCode(AutomationFailureClass failureClass, string code)
    {
        AutomationFailureClassification.TryParse(code, out var parsed).Should().BeTrue();
        parsed.Should().Be(failureClass);
    }

    [Theory]
    [InlineData("Transient")]
    [InlineData("TARGET_GONE")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_RefusesAnUnknownCode(string? code)
    {
        AutomationFailureClassification.TryParse(code, out _).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ClassCodes))]
    public void IsRetryable_OnlyTransientIsRetryable(AutomationFailureClass failureClass, string _)
    {
        failureClass.IsRetryable().Should().Be(failureClass == AutomationFailureClass.Transient);
    }

    [Theory]
    [MemberData(nameof(TerminalClasses))]
    public void Fail_RecordsEachTerminalClassAndTheHumanDetail(AutomationFailureClass failureClass, string _)
    {
        var execution = RunningExecution();

        execution.Fail(failureClass, "The target group no longer exists.", DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Failed);
        execution.FailureClass.Should().Be(failureClass);
        execution.Error.Should().Be("The target group no longer exists.");
    }

    [Fact]
    public void Fail_WithTransient_IsRefused_BecauseATransientFailureIsRetried()
    {
        var execution = RunningExecution();

        var act = () => execution.Fail(AutomationFailureClass.Transient, "timeout", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Execution_FailureClassNotTerminal);
        execution.Status.Should().Be(AutomationExecutionStatus.Running);
        execution.FailureClass.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(TerminalClasses))]
    public void RecordRetry_IsRefusedForEachTerminalClass(AutomationFailureClass failureClass, string _)
    {
        var execution = RunningExecution();

        var act = () => execution.RecordRetryableDispatchFailure(failureClass, "provider said no", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Execution_FailureClassNotRetryable);
        execution.Status.Should().Be(AutomationExecutionStatus.Running);
        execution.AttemptCount.Should().Be(0);
    }

    [Fact]
    public void RecordRetry_WithTransient_RequeuesAndKeepsTheClass()
    {
        var execution = RunningExecution();

        execution.RecordRetryableDispatchFailure(AutomationFailureClass.Transient, "n8n unreachable", DateTimeOffset.UtcNow);

        execution.Status.Should().Be(AutomationExecutionStatus.Queued);
        execution.AttemptCount.Should().Be(1);
        execution.FailureClass.Should().Be(AutomationFailureClass.Transient);
    }
}
