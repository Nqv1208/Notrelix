using FluentAssertions;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Agents.Events;

namespace Notrelix.Domain.Tests.Automation;

public class AiAgentRunVersionTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Start_ShouldIncrementVersion()
    {
        var run = AiAgentRun.Create(_workspaceId, Guid.NewGuid(), "webhook", null, null, JsonValue.Null(), _actorId, null, _now);
        run.ClearDomainEvents();
        var version = run.Version;

        run.Start(_now);

        run.Version.Should().Be(version + 1);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunStartedDomainEvent);
    }

    [Fact]
    public void Succeed_ShouldIncrementVersion()
    {
        var run = AiAgentRun.Create(_workspaceId, Guid.NewGuid(), "webhook", null, null, JsonValue.Null(), _actorId, null, _now);
        run.Start(_now);
        run.ClearDomainEvents();
        var version = run.Version;

        run.Succeed(JsonValue.Null(), _now);

        run.Version.Should().Be(version + 1);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunSucceededDomainEvent);
    }

    [Fact]
    public void Fail_ShouldIncrementVersion()
    {
        var run = AiAgentRun.Create(_workspaceId, Guid.NewGuid(), "webhook", null, null, JsonValue.Null(), _actorId, null, _now);
        run.Start(_now);
        run.ClearDomainEvents();
        var version = run.Version;

        run.Fail(JsonValue.Null(), _now);

        run.Version.Should().Be(version + 1);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunFailedDomainEvent);
    }

    [Fact]
    public void Cancel_ShouldIncrementVersion()
    {
        var run = AiAgentRun.Create(_workspaceId, Guid.NewGuid(), "webhook", null, null, JsonValue.Null(), _actorId, null, _now);
        run.Start(_now);
        run.ClearDomainEvents();
        var version = run.Version;

        run.Cancel(_actorId, _now);

        run.Version.Should().Be(version + 1);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunCancelledDomainEvent);
    }
}
