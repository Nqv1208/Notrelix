using Notrelix.Application.Common.Events;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Application.Features.Automation.Jobs;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Items.Events;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Integration.Tests.Extensibility;

public class N8nAutomationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task CardAssignedN8nAutomationHandler_ShouldCreateExecutionAndQueueDispatchJob()
    {
        await using var context = CreateContext();
        var queue = new CapturingJobQueue();
        var ownerId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var workspace = Workspace.Create(ownerId, "Workspace", "workspace", Now);
        context.Workspaces.Add(workspace);

        var ownerMember = WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now);
        var assignedMember = WorkspaceMember.Create(workspace.Id, assignedUserId, WorkspaceRole.Member, ownerId, Now);
        context.WorkspaceMembers.Add(ownerMember);
        context.WorkspaceMembers.Add(assignedMember);

        var board = Board.Create(workspace.Id, ownerId, "Board", null, Now);
        context.Boards.Add(board);

        var groupId = Guid.NewGuid();
        var item = BoardItem.Create(workspace.Id, board.Id, groupId, "Task", Notrelix.Domain.SharedKernel.FractionalIndex.Initial(), ownerId, Now);
        context.BoardItems.Add(item);

        var trigger = AutomationTriggerDefinition.Create("ItemAssigned");
        var action = AutomationActionDefinition.Create("Webhook", """{"webhookPath":"notrelix-card-assigned"}""");
        var config = AutomationConfiguration.Create(trigger, action);
        var rule = AutomationRule.Create(workspace.Id, "Card assigned alert", config, ownerId, Now);
        rule.Enable(ownerId, Now);
        context.AutomationRules.Add(rule);

        await context.SaveChangesAsync();

        var handler = new CardAssignedN8nAutomationHandler(context, queue);
        var domainEvent = new BoardItemMemberAssignedDomainEvent(
            workspace.Id, item.Id, assignedUserId, ownerId, Now);

        await handler.Handle(
            new DomainEventNotification<BoardItemMemberAssignedDomainEvent>(domainEvent),
            CancellationToken.None);

        var execution = await context.AutomationExecutions.SingleAsync();
        execution.WorkspaceId.Should().Be(workspace.Id);
        execution.RuleId.Should().Be(rule.Id);
        execution.TriggerId.Should().Be(domainEvent.EventId);
        execution.Status.Should().Be(AutomationExecutionStatus.Queued);

        var job = queue.Jobs.Should().ContainSingle().Subject.Should().BeOfType<N8nDispatchJob>().Subject;
        job.ExecutionId.Should().Be(execution.Id);
        job.AutomationRuleId.Should().Be(rule.Id);
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
