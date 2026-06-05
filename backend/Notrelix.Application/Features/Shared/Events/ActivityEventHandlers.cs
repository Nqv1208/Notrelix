using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Events;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Entities.Shared;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;
using Notrelix.Domain.Events.Workspace;

namespace Notrelix.Application.Features.Shared.Events;

public class BoardCreatedActivityHandler : INotificationHandler<DomainEventNotification<BoardCreatedEvent>>
{
    private readonly IApplicationDbContext _context;

    public BoardCreatedActivityHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEventNotification<BoardCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _context.ActivityLogs.Add(ActivityLog.Create(
            domainEvent.WorkspaceId,
            domainEvent.CreatedBy,
            "board.created",
            ResourceType.Board,
            domainEvent.BoardId,
            domainEvent.Title));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class CardCreatedActivityHandler : INotificationHandler<DomainEventNotification<CardCreatedEvent>>
{
    private readonly IApplicationDbContext _context;

    public CardCreatedActivityHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEventNotification<CardCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var workspaceId = await _context.Boards
            .Where(board => board.Id == domainEvent.BoardId)
            .Select(board => board.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (workspaceId == Guid.Empty) return;

        _context.ActivityLogs.Add(ActivityLog.Create(
            workspaceId,
            domainEvent.CreatedBy,
            "card.created",
            ResourceType.Card,
            domainEvent.CardId,
            domainEvent.Title));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class CardMovedActivityHandler : INotificationHandler<DomainEventNotification<CardMovedEvent>>
{
    private readonly IApplicationDbContext _context;

    public CardMovedActivityHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEventNotification<CardMovedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var card = await _context.Cards
            .AsNoTracking()
            .Where(item => item.Id == domainEvent.CardId)
            .Select(item => new { item.Id, item.Title, item.CreatedByUserId, BoardId = item.List.BoardId })
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null) return;

        var workspaceId = await _context.Boards
            .Where(board => board.Id == card.BoardId)
            .Select(board => board.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (workspaceId == Guid.Empty) return;

        _context.ActivityLogs.Add(ActivityLog.Create(
            workspaceId,
            card.CreatedByUserId,
            "card.moved",
            ResourceType.Card,
            card.Id,
            card.Title));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class WorkspaceMemberInvitedActivityHandler : INotificationHandler<DomainEventNotification<MemberInvitedEvent>>
{
    private readonly IApplicationDbContext _context;

    public WorkspaceMemberInvitedActivityHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DomainEventNotification<MemberInvitedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _context.ActivityLogs.Add(ActivityLog.Create(
            domainEvent.WorkspaceId,
            domainEvent.InvitedBy,
            "workspace.member_invited",
            ResourceType.Workspace,
            domainEvent.WorkspaceId,
            domainEvent.Email));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
