using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Events;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Calendar.Events;
using Notrelix.Application.Features.Calendar.Jobs;
using Notrelix.Application.Features.Shared.Events;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Application.Tests.Events;

public class DomainEventHandlerTests
{
    [Fact]
    public async Task BoardCreatedActivityHandler_ShouldCreateActivityLog()
    {
        await using var context = CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var handler = new BoardCreatedActivityHandler(context);

        await handler.Handle(
            new DomainEventNotification<BoardCreatedEvent>(
                new BoardCreatedEvent(boardId, workspaceId, actorId, "Roadmap")),
            CancellationToken.None);

        var activity = await context.ActivityLogs.SingleAsync();
        activity.WorkspaceId.Should().Be(workspaceId);
        activity.ActorId.Should().Be(actorId);
        activity.Action.Should().Be("board.created");
        activity.ResourceType.Should().Be(ResourceType.Board);
        activity.ResourceId.Should().Be(boardId);
    }

    [Fact]
    public async Task CardAssignedNotificationHandler_ShouldCreateNotificationForAssignedUser()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Workspace", ownerId);
        workspace.AddMember(assignedUserId, WorkspaceRole.Member);
        var board = Board.Create(workspace.Id, ownerId, "Board", null);
        var list = BoardList.Create(board.Id, "Todo", 1024);
        var card = Card.Create(list.Id, board.Id, ownerId, "Task", 1024);
        context.Workspaces.Add(workspace);
        context.Boards.Add(board);
        context.BoardLists.Add(list);
        context.Cards.Add(card);
        await context.SaveChangesAsync();
        var handler = new CardAssignedNotificationHandler(context);

        await handler.Handle(
            new DomainEventNotification<CardAssignedEvent>(
                new CardAssignedEvent(card.Id, assignedUserId, ownerId)),
            CancellationToken.None);

        var notification = await context.Notifications.SingleAsync();
        notification.WorkspaceId.Should().Be(workspace.Id);
        notification.UserId.Should().Be(assignedUserId);
        notification.ActorId.Should().Be(ownerId);
        notification.Type.Should().Be("card.assigned");
        notification.ResourceType.Should().Be(ResourceType.Card);
        notification.ResourceId.Should().Be(card.Id);
    }

    [Fact]
    public async Task CardDueDateCalendarHandler_ShouldEnqueueCardCalendarSyncJob()
    {
        var queue = new CapturingJobQueue();
        var handler = new CardDueDateCalendarHandler(queue);
        var cardId = Guid.NewGuid();

        await handler.Handle(
            new DomainEventNotification<CardDueDateChangedEvent>(
                new CardDueDateChangedEvent(cardId, null, DateTime.UtcNow.AddDays(1), Guid.NewGuid())),
            CancellationToken.None);

        var job = queue.Jobs.Should().ContainSingle().Subject.Should().BeOfType<CalendarSyncJob>().Subject;
        job.ResourceType.Should().Be(ResourceType.Card);
        job.ResourceId.Should().Be(cardId);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-events-{Guid.NewGuid():N}")
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
