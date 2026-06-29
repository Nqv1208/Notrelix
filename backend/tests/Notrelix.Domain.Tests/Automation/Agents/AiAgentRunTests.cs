using FluentAssertions;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Agents.Events;

namespace Notrelix.Domain.Tests.Automation;

public class AiAgentRunTests
{
    private static readonly JsonValue SampleInput = JsonValue.Create("{\"text\":\"hello\"}");

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var run = CreateRun();

        run.Status.Should().Be(AiAgentRunStatus.Queued);
        run.DomainEvents.Should().ContainSingle(e => e is AiAgentRunQueuedDomainEvent);
    }

    [Fact]
    public void Start_ShouldTransition_AndRaiseEvent()
    {
        var run = CreateRun();
        run.ClearDomainEvents();

        run.Start(DateTimeOffset.UtcNow);

        run.Status.Should().Be(AiAgentRunStatus.Running);
        run.DomainEvents.Should().ContainSingle(e => e is AiAgentRunStartedDomainEvent);
    }

    [Fact]
    public void Start_WhenNotQueued_ShouldThrow()
    {
        var run = CreateRun();
        run.Start(DateTimeOffset.UtcNow);

        var act = () => run.Start(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Queued*");
    }

    [Fact]
    public void Start_WhenCancelled_ShouldThrow()
    {
        var run = CreateRun();
        run.Cancel(null, DateTimeOffset.UtcNow);

        var act = () => run.Start(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Queued*");
    }

    [Fact]
    public void Succeed_ShouldTransition_AndRaiseEvent()
    {
        var run = CreateRun();
        run.Start(DateTimeOffset.UtcNow);
        run.ClearDomainEvents();

        run.Succeed(JsonValue.Create("{\"result\":\"ok\"}"), DateTimeOffset.UtcNow);

        run.Status.Should().Be(AiAgentRunStatus.Succeeded);
        run.DomainEvents.Should().ContainSingle(e => e is AiAgentRunSucceededDomainEvent);
    }

    [Fact]
    public void Succeed_WhenNotRunning_ShouldThrow()
    {
        var run = CreateRun();

        var act = () => run.Succeed(JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Running*");
    }

    [Fact]
    public void Succeed_WithNullOutput_ShouldThrow()
    {
        var run = CreateRun();
        run.Start(DateTimeOffset.UtcNow);

        var act = () => run.Succeed(null!, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Fail_ShouldTransition_AndRaiseEvent()
    {
        var run = CreateRun();
        run.Start(DateTimeOffset.UtcNow);
        run.ClearDomainEvents();

        run.Fail(JsonValue.Create("{\"error\":\"timeout\"}"), DateTimeOffset.UtcNow);

        run.Status.Should().Be(AiAgentRunStatus.Failed);
        run.DomainEvents.Should().ContainSingle(e => e is AiAgentRunFailedDomainEvent);
    }

    [Fact]
    public void Fail_WhenNotRunning_ShouldThrow()
    {
        var run = CreateRun();

        var act = () => run.Fail(JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Running*");
    }

    [Fact]
    public void Fail_WithNullError_ShouldThrow()
    {
        var run = CreateRun();
        run.Start(DateTimeOffset.UtcNow);

        var act = () => run.Fail(null!, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Cancel_FromQueued_ShouldTransition()
    {
        var run = CreateRun();

        run.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow);

        run.Status.Should().Be(AiAgentRunStatus.Cancelled);
        run.DomainEvents.Should().ContainSingle(e => e is AiAgentRunCancelledDomainEvent);
    }

    [Fact]
    public void Cancel_FromRunning_ShouldTransition()
    {
        var run = CreateRun();
        run.Start(DateTimeOffset.UtcNow);
        run.ClearDomainEvents();

        run.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow);

        run.Status.Should().Be(AiAgentRunStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadySucceeded_ShouldThrow()
    {
        var run = CreateRun();
        run.Start(DateTimeOffset.UtcNow);
        run.Succeed(JsonValue.EmptyObject(), DateTimeOffset.UtcNow);

        var act = () => run.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Cancel_WhenQueued_ShouldNotDelete()
    {
        var run = CreateRun();
        run.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow);

        run.IsDeleted.Should().BeFalse();
    }

    private static AiAgentRun CreateRun()
    {
        return AiAgentRun.Create(
            Guid.NewGuid(), Guid.NewGuid(), "manual", "board", Guid.NewGuid(),
            SampleInput, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
