using Microsoft.EntityFrameworkCore;
using Moq;
using Notrelix.Application.Common.Events;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Application.Features.Automation.Jobs;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Extensibility;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Application.Tests.Extensibility;

public class N8nAutomationTests
{
    private static AutomationConfiguration CreateN8nConfig()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemAssigned");
        var action = AutomationActionDefinition.Create("Webhook", """{"webhookPath":"notrelix-card-assigned"}""");
        return AutomationConfiguration.Create(trigger, action);
    }

    [Fact]
    public async Task CreateAutomationRule_ShouldRequireWorkspaceManagePermission()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Workspace", ownerId);
        workspace.AddMember(memberId, WorkspaceRole.Member);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var handler = new CreateAutomationRuleCommandHandler(
            context,
            CurrentUser(memberId),
            new WorkspacePermissionService(context));

        var act = () => handler.Handle(
            new CreateAutomationRuleCommand(
                workspace.Id,
                "Card assigned alert",
                "ItemAssigned",
                "Webhook",
                """{"webhookPath":"notrelix-card-assigned"}"""),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CardAssignedN8nAutomationHandler_ShouldCreateExecutionAndQueueDispatchJob()
    {
        await using var context = CreateContext();
        var queue = new CapturingJobQueue();
        var ownerId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Workspace", ownerId);
        workspace.AddMember(assignedUserId, WorkspaceRole.Member);
        var board = Board.Create(workspace.Id, ownerId, "Board", null);
        var list = BoardList.Create(board.Id, "Todo", 1024);
        var card = Card.Create(list.Id, board.Id, ownerId, "Task", 1024);
        var config = CreateN8nConfig();
        var rule = AutomationRule.Create(
            workspace.Id,
            "Card assigned alert",
            config,
            ownerId,
            DateTimeOffset.UtcNow);

        context.Workspaces.Add(workspace);
        context.Boards.Add(board);
        context.BoardLists.Add(list);
        context.Cards.Add(card);
        context.AutomationRules.Add(rule);
        await context.SaveChangesAsync();

        var handler = new CardAssignedN8nAutomationHandler(context, queue);
        var domainEvent = new CardAssignedEvent(card.Id, assignedUserId, ownerId);

        await handler.Handle(
            new DomainEventNotification<CardAssignedEvent>(domainEvent),
            CancellationToken.None);

        var execution = await context.AutomationExecutions.SingleAsync();
        execution.WorkspaceId.Should().Be(workspace.Id);
        execution.AutomationRuleId.Should().Be(rule.Id);
        execution.EventId.Should().Be(domainEvent.EventId);
        execution.EventType.Should().Be("card.assigned");
        execution.ResourceType.Should().Be(ResourceType.Card);
        execution.ResourceId.Should().Be(card.Id);
        execution.Status.Should().Be(AutomationExecutionStatus.Pending);
        execution.Payload.Should().Contain(assignedUserId.ToString());

        var job = queue.Jobs.Should().ContainSingle().Subject.Should().BeOfType<N8nDispatchJob>().Subject;
        job.ExecutionId.Should().Be(execution.Id);
        job.AutomationRuleId.Should().Be(rule.Id);
    }

    private static ICurrentUser CurrentUser(Guid userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(userId);
        return currentUser.Object;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-n8n-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class CapturingJobQueue : IJobQueue
    {
        public List<object> Jobs { get; } = [];

        public Task EnqueueAsync<TJob>(TJob job, CancellationToken cancellationToken = default) where TJob : class
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task EnqueueAsync<TJob>(TJob job, TimeSpan delay, CancellationToken cancellationToken = default) where TJob : class
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }
    }
}
