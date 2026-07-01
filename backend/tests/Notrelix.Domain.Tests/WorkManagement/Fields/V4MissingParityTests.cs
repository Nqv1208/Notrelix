using FluentAssertions;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Agents.Events;
using Notrelix.Domain.Analytics.Dashboards;

namespace Notrelix.Domain.Tests.WorkManagement;

public class V4MissingParityTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void AiAgent_Create_ShouldSucceed_AndRaiseEvent()
    {
        var agent = AiAgent.Create(Guid.NewGuid(),
            _workspaceId,
            "Translation Bot",
            "Translates items automatically",
            AiAgentScopeType.Board,
            _boardId,
            JsonValue.Create("{\"model\": \"gemini-1.5-pro\"}"),
            JsonValue.Create("{\"system_instruction\": \"Translate to French\"}"),
            JsonValue.Create("{\"permissions\": [\"read\", \"write\"]}"),
            _actorId,
            _now);

        agent.Should().NotBeNull();
        agent.WorkspaceId.Should().Be(_workspaceId);
        agent.Name.Should().Be("Translation Bot");
        agent.Description.Should().Be("Translates items automatically");
        agent.ScopeType.Should().Be(AiAgentScopeType.Board);
        agent.ScopeResourceId.Should().Be(_boardId);
        agent.Status.Should().Be(AiAgentStatus.Draft);
        agent.Version.Should().Be(1);

        agent.DomainEvents.Should().ContainSingle(e => e is AiAgentCreatedDomainEvent);
        var evt = (AiAgentCreatedDomainEvent)agent.DomainEvents.First();
        evt.WorkspaceId.Should().Be(_workspaceId);
        evt.AgentId.Should().Be(agent.Id);
        evt.Name.Should().Be("Translation Bot");
        evt.ActorUserId.Should().Be(_actorId);
    }

    [Fact]
    public void AiAgent_Update_ShouldModifyFields_AndRaiseEvent()
    {
        var agent = AiAgent.Create(Guid.NewGuid(),
            _workspaceId,
            "Agent",
            null,
            AiAgentScopeType.Workspace,
            null,
            JsonValue.EmptyObject(),
            JsonValue.EmptyObject(),
            JsonValue.EmptyObject(),
            _actorId,
            _now);

        agent.Update(
            "Updated Agent",
            "New description",
            JsonValue.Create("{\"model\":\"updated\"}"),
            JsonValue.Create("{\"instruction\":\"updated\"}"),
            JsonValue.Create("{\"permissions\":\"updated\"}"),
            _actorId,
            _now);

        agent.Name.Should().Be("Updated Agent");
        agent.Description.Should().Be("New description");
        agent.Version.Should().Be(2);

        agent.DomainEvents.Should().Contain(e => e is AiAgentUpdatedDomainEvent);
    }

    [Fact]
    public void AiAgent_ChangeStatus_ShouldTransitionStatus()
    {
        var agent = AiAgent.Create(Guid.NewGuid(),
            _workspaceId,
            "Agent",
            null,
            AiAgentScopeType.Workspace,
            null,
            JsonValue.EmptyObject(),
            JsonValue.EmptyObject(),
            JsonValue.EmptyObject(),
            _actorId,
            _now);

        agent.ChangeStatus(AiAgentStatus.Enabled, _actorId, _now);
        agent.Status.Should().Be(AiAgentStatus.Enabled);
        agent.Version.Should().Be(2);

        agent.ChangeStatus(AiAgentStatus.Deleted, _actorId, _now);
        agent.Status.Should().Be(AiAgentStatus.Deleted);
        agent.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void AiAgentRun_CreateAndTransition_ShouldManageLifecycle()
    {
        var agentId = Guid.NewGuid();
        var run = AiAgentRun.Create(Guid.NewGuid(),
            _workspaceId,
            agentId,
            "ItemCreated",
            "BoardItem",
            Guid.NewGuid(),
            JsonValue.Create("{\"itemId\":\"123\"}"),
            _actorId,
            Guid.NewGuid(),
            _now);

        run.Status.Should().Be(AiAgentRunStatus.Queued);
        run.DomainEvents.Should().ContainSingle(e => e is AiAgentRunQueuedDomainEvent);

        // Transition: Queued -> Running
        run.Start(_now.AddSeconds(1));
        run.Status.Should().Be(AiAgentRunStatus.Running);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunStartedDomainEvent);

        // Transition: Running -> Succeeded
        var output = JsonValue.Create("{\"result\":\"success\"}");
        run.Succeed(output, _now.AddSeconds(5));
        run.Status.Should().Be(AiAgentRunStatus.Succeeded);
        run.Output.Should().Be(output);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunSucceededDomainEvent);
    }

    [Fact]
    public void DashboardSource_Create_ShouldSucceed()
    {
        var dashboardId = Guid.NewGuid();
        var filter = JsonValue.Create("{\"status\":\"Done\"}");

        var source = DashboardSource.Create(Guid.NewGuid(),
            _workspaceId,
            dashboardId,
            DashboardSourceType.BoardView,
            _boardId,
            Guid.NewGuid(),
            filter,
            _actorId,
            _now);

        source.WorkspaceId.Should().Be(_workspaceId);
        source.DashboardId.Should().Be(dashboardId);
        source.SourceType.Should().Be(DashboardSourceType.BoardView);
        source.BoardId.Should().Be(_boardId);
        source.Filter.Should().Be(filter);

        source.UpdateFilter(JsonValue.Create("{\"status\":\"All\"}"), _actorId, _now.AddSeconds(1));
        source.Filter.Should().Be(JsonValue.Create("{\"status\":\"All\"}"));
        source.Version.Should().Be(2);
    }
}
