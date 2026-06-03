using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Events;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Entities.Shared;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;
using Notrelix.Domain.Events.Document;
using Notrelix.Domain.Events.Shared;

namespace Notrelix.Application.Features.Shared.Events;

public class CardAssignedNotificationHandler : INotificationHandler<DomainEventNotification<CardAssignedEvent>>
{
    private readonly IApplicationDbContext _context;

    public CardAssignedNotificationHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEventNotification<CardAssignedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var card = await _context.Cards
            .AsNoTracking()
            .Where(item => item.Id == domainEvent.CardId)
            .Select(item => new { item.Id, item.Title, BoardId = item.List.BoardId })
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null) return;

        var workspaceId = await _context.Boards
            .Where(board => board.Id == card.BoardId)
            .Select(board => board.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (workspaceId == Guid.Empty) return;

        _context.Notifications.Add(Notification.Create(
            workspaceId,
            domainEvent.AssignedUserId,
            "card.assigned",
            domainEvent.AssignedBy,
            JsonSerializer.Serialize(new { cardId = card.Id, title = card.Title }),
            ResourceType.Card,
            card.Id));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class PageMentionedNotificationHandler : INotificationHandler<DomainEventNotification<PageMentionedEvent>>
{
    private readonly IApplicationDbContext _context;

    public PageMentionedNotificationHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEventNotification<PageMentionedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var page = await _context.Pages
            .AsNoTracking()
            .Where(item => item.Id == domainEvent.PageId)
            .Select(item => new { item.Id, item.Title, item.WorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (page is null) return;

        _context.Notifications.Add(Notification.Create(
            page.WorkspaceId,
            domainEvent.MentionedUserId,
            "page.mentioned",
            domainEvent.MentionedBy,
            JsonSerializer.Serialize(new { pageId = page.Id, blockId = domainEvent.BlockId, title = page.Title }),
            ResourceType.Page,
            page.Id));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class CommentCreatedNotificationHandler : INotificationHandler<DomainEventNotification<CommentCreatedEvent>>
{
    private readonly IApplicationDbContext _context;

    public CommentCreatedNotificationHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEventNotification<CommentCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        if (domainEvent.ResourceType != ResourceType.Card) return;

        var cardMembers = await _context.CardMembers
            .AsNoTracking()
            .Where(member => member.CardId == domainEvent.ResourceId && member.UserId != domainEvent.CreatedBy)
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);

        foreach (var userId in cardMembers.Distinct())
        {
            _context.Notifications.Add(Notification.Create(
                domainEvent.WorkspaceId,
                userId,
                "comment.created",
                domainEvent.CreatedBy,
                JsonSerializer.Serialize(new { commentId = domainEvent.CommentId, resourceId = domainEvent.ResourceId }),
                domainEvent.ResourceType,
                domainEvent.ResourceId));
        }

        if (cardMembers.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class NotificationCreatedRealtimeHandler : INotificationHandler<DomainEventNotification<NotificationCreatedEvent>>
{
    private readonly INotificationService _notificationService;

    public NotificationCreatedRealtimeHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Handle(DomainEventNotification<NotificationCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        try
        {
            await _notificationService.SendAsync(
                domainEvent.UserId,
                domainEvent.Type,
                JsonSerializer.Serialize(new
                {
                    notificationId = domainEvent.NotificationId,
                    workspaceId = domainEvent.WorkspaceId,
                    resourceType = domainEvent.ResourceType?.ToString(),
                    resourceId = domainEvent.ResourceId
                }),
                cancellationToken);
        }
        catch
        {
            // Realtime delivery is best-effort; the database notification is the durable source.
        }
    }
}
