using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Events;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Extensibility.Jobs;
using Notrelix.Domain.Entities.Extensibility;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Application.Features.Extensibility.Events;

public class CardAssignedN8nAutomationHandler : INotificationHandler<DomainEventNotification<CardAssignedEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJobQueue _jobQueue;

    public CardAssignedN8nAutomationHandler(IApplicationDbContext context, IJobQueue jobQueue)
    {
        _context = context;
        _jobQueue = jobQueue;
    }

    public async Task Handle(DomainEventNotification<CardAssignedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var cardContext = await _context.Cards
            .AsNoTracking()
            .Where(card => card.Id == domainEvent.CardId && !card.IsDeleted)
            .Select(card => new
            {
                card.Id,
                card.Title,
                BoardId = card.List.BoardId,
                WorkspaceId = card.List.Board.WorkspaceId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cardContext is null) return;

        var rules = await _context.AutomationRules
            .Where(rule =>
                rule.WorkspaceId == cardContext.WorkspaceId &&
                rule.IsEnabled &&
                rule.TriggerEvent == "card.assigned" &&
                rule.ActionType == "n8n.webhook")
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            var exists = await _context.AutomationExecutions
                .AsNoTracking()
                .AnyAsync(execution =>
                    execution.AutomationRuleId == rule.Id &&
                    execution.EventId == domainEvent.EventId,
                    cancellationToken);

            if (exists) continue;

            var payload = JsonSerializer.Serialize(new
            {
                eventId = domainEvent.EventId,
                eventType = "card.assigned",
                workspaceId = cardContext.WorkspaceId,
                resourceType = ResourceType.Card.ToString(),
                resourceId = cardContext.Id,
                actorId = domainEvent.AssignedBy,
                occurredAt = domainEvent.OccurredOn,
                data = new
                {
                    cardId = cardContext.Id,
                    cardTitle = cardContext.Title,
                    boardId = cardContext.BoardId,
                    assignedUserId = domainEvent.AssignedUserId,
                    assignedBy = domainEvent.AssignedBy
                }
            });

            var execution = AutomationExecution.CreatePending(
                cardContext.WorkspaceId,
                rule.Id,
                domainEvent.EventId,
                "card.assigned",
                ResourceType.Card,
                cardContext.Id,
                payload);

            _context.AutomationExecutions.Add(execution);
            await _context.SaveChangesAsync(cancellationToken);

            await _jobQueue.EnqueueAsync(new N8nDispatchJob(execution.Id, rule.Id), cancellationToken);
        }
    }
}
