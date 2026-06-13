using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Agents.Events;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Analytics.Dashboards;
using Xunit;

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
        var agent = AiAgent.Create(
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

        agent.DomainEvents.Should().ContainSingle(e => e is AiAgentCreatedEvent);
        var evt = (AiAgentCreatedEvent)agent.DomainEvents.First();
        evt.WorkspaceId.Should().Be(_workspaceId);
        evt.AgentId.Should().Be(agent.Id);
        evt.Name.Should().Be("Translation Bot");
        evt.ActorUserId.Should().Be(_actorId);
    }

    [Fact]
    public void AiAgent_Update_ShouldModifyFields_AndRaiseEvent()
    {
        var agent = AiAgent.Create(
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

        agent.DomainEvents.Should().Contain(e => e is AiAgentUpdatedEvent);
    }

    [Fact]
    public void AiAgent_ChangeStatus_ShouldTransitionStatus()
    {
        var agent = AiAgent.Create(
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
        var run = AiAgentRun.Create(
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
        run.DomainEvents.Should().ContainSingle(e => e is AiAgentRunQueuedEvent);

        // Transition: Queued -> Running
        run.Start(_now.AddSeconds(1));
        run.Status.Should().Be(AiAgentRunStatus.Running);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunStartedEvent);

        // Transition: Running -> Succeeded
        var output = JsonValue.Create("{\"result\":\"success\"}");
        run.Succeed(output, _now.AddSeconds(5));
        run.Status.Should().Be(AiAgentRunStatus.Succeeded);
        run.Output.Should().Be(output);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunSucceededEvent);
    }

    [Fact]
    public void NotificationDelivery_Lifecycle_ShouldSucceed()
    {
        var notificationId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        
        var delivery = NotificationDelivery.Create(
            notificationId,
            _workspaceId,
            recipientUserId,
            NotificationChannel.Email,
            _now);

        delivery.Status.Should().Be(NotificationDeliveryStatus.Pending);
        delivery.WorkspaceId.Should().Be(_workspaceId);
        delivery.RecipientUserId.Should().Be(recipientUserId);

        delivery.MarkSent("msg-12345", _now.AddSeconds(2));
        delivery.Status.Should().Be(NotificationDeliveryStatus.Sent);
        delivery.ProviderMessageId.Should().Be("msg-12345");
    }

    [Fact]
    public void UnreadCounter_ShouldIncrementAndReset()
    {
        var userId = Guid.NewGuid();
        var counter = UnreadCounter.Create(_workspaceId, userId, UnreadCounterType.Mention, _now);

        counter.CounterValue.Should().Be(0);
        
        counter.Increment(_now.AddSeconds(1));
        counter.CounterValue.Should().Be(1);

        counter.Increment(_now.AddSeconds(2));
        counter.CounterValue.Should().Be(2);

        counter.Decrement(_now.AddSeconds(3));
        counter.CounterValue.Should().Be(1);

        counter.Reset(_now.AddSeconds(4));
        counter.CounterValue.Should().Be(0);
    }

    [Fact]
    public void DashboardSource_Create_ShouldSucceed()
    {
        var dashboardId = Guid.NewGuid();
        var filter = JsonValue.Create("{\"status\":\"Done\"}");

        var source = DashboardSource.Create(
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
