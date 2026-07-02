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
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Extensibility;

[Collection("Database")]
public class N8nAutomationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public N8nAutomationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task CardAssignedN8nAutomationHandler_ShouldCreateExecutionAndQueueDispatchJob()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var queue = new CapturingJobQueue();
        var ownerId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Workspace", "workspace", Now);
        context.Workspaces.Add(workspace);

        var ownerMember = WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now);
        var assignedMember = WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, assignedUserId, WorkspaceRole.Member, ownerId, Now);
        context.WorkspaceMembers.Add(ownerMember);
        context.WorkspaceMembers.Add(assignedMember);

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, Now);
        context.Boards.Add(board);

        var group = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup.Create(Guid.NewGuid(), workspace.Id, board.Id, "Todo", Notrelix.Domain.SharedKernel.Color.Create("#808080"), Notrelix.Domain.SharedKernel.FractionalIndex.Initial(), ownerId, Now);
        context.BoardGroups.Add(group);

        var item = BoardItem.Create(Guid.NewGuid(), workspace.Id, board.Id, group.Id, "Task", Notrelix.Domain.SharedKernel.FractionalIndex.Initial(), ownerId, Now);
        context.BoardItems.Add(item);

        var trigger = AutomationTriggerDefinition.Create("ItemAssigned");
        var action = AutomationActionDefinition.Create("Webhook", """{"webhookPath":"notrelix-card-assigned"}""");
        var config = AutomationConfiguration.Create(trigger, action);
        var rule = AutomationRule.Create(Guid.NewGuid(), workspace.Id, "Card assigned alert", config, ownerId, Now);
        rule.Enable(ownerId, Now);
        context.AutomationRules.Add(rule);

        await context.SaveChangesAsync();

        var resourceResolver = new TestResourceReferenceResolver(context);
        var handler = new CardAssignedN8nAutomationHandler(context, resourceResolver, queue);
        var domainEvent = new BoardItemMemberAssignedDomainEvent(
            Guid.NewGuid(), workspace.Id, item.Id, assignedUserId, ownerId, Now);

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

    private sealed class TestResourceReferenceResolver : IResourceReferenceResolver
    {
        private readonly ApplicationDbContext _context;
        public TestResourceReferenceResolver(ApplicationDbContext context) => _context = context;

        public async Task<Guid?> GetWorkspaceIdAsync(Guid resourceId, string resourceType, CancellationToken ct)
        {
            if (resourceType == ResourceTypes.BoardItem)
            {
                var item = await _context.BoardItems.FindAsync([resourceId], ct);
                return item?.WorkspaceId;
            }
            return null;
        }

        public async Task<bool> ExistsAsync(Guid resourceId, string resourceType, CancellationToken ct)
            => (await GetWorkspaceIdAsync(resourceId, resourceType, ct)).HasValue;

        public async Task<AccountContextSnapshot?> GetAccountContextAsync(Guid resourceId, string resourceType, CancellationToken ct)
        {
            if (resourceType == ResourceTypes.BoardItem)
            {
                var item = await _context.BoardItems.FindAsync([resourceId], ct);
                if (item is null) return null;
                return new AccountContextSnapshot(item.AccountId, item.WorkspaceId);
            }
            return null;
        }
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
