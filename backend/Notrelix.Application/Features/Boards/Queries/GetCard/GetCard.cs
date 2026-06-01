using BoardEntity = global::Notrelix.Domain.Entities.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Boards.Queries.GetCard;

public record GetCardQuery(Guid CardId) : IRequest<Result<CardDto>>;

public class GetCardQueryHandler : IRequestHandler<GetCardQuery, Result<CardDto>>
{
    private readonly IApplicationDbContext _context;
    public GetCardQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<CardDto>> Handle(GetCardQuery request, CancellationToken ct)
    {
        var card = await _context.Cards.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CardId && !c.IsDeleted, ct);

        if (card is null) throw new NotFoundException("Card", request.CardId);

        var members = await _context.CardMembers.AsNoTracking()
            .Where(m => m.CardId == card.Id)
            .Join(_context.Users.AsNoTracking(), m => m.UserId, u => u.Id,
                (m, u) => new CardMemberDto(m.UserId, u.Name, u.AvatarUrl, m.AssignedAt))
            .ToListAsync(ct);

        var labels = await _context.CardLabels.AsNoTracking()
            .Where(cl => cl.CardId == card.Id)
            .Join(_context.Labels.AsNoTracking(), cl => cl.LabelId, l => l.Id,
                (cl, l) => new CardLabelDto(l.Id, l.Name, l.Color))
            .ToListAsync(ct);

        var checklists = await _context.Checklists.AsNoTracking()
            .Where(c => c.CardId == card.Id)
            .OrderBy(c => c.Position)
            .Select(c => new ChecklistDto(
                c.Id, c.Title, c.Position,
                _context.ChecklistItems.AsNoTracking()
                    .Where(i => i.ChecklistId == c.Id)
                    .OrderBy(i => i.Position)
                    .Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsChecked, i.DueDate, i.AssigneeId, i.Position))
                    .ToList()
            )).ToListAsync(ct);

        var listContext = await _context.BoardLists.AsNoTracking()
            .Where(list => list.Id == card.ListId)
            .Join(_context.Boards.AsNoTracking(),
                list => list.BoardId,
                board => board.Id,
                (list, board) => new { BoardId = board.Id, board.WorkspaceId })
            .FirstOrDefaultAsync(ct);

        if (listContext is null) throw new NotFoundException("List", card.ListId);

        var commentCount = await _context.Comments.AsNoTracking()
            .CountAsync(comment => comment.ResourceId == card.Id && !comment.IsDeleted, ct);
        var attachmentCount = await _context.Attachments.AsNoTracking()
            .CountAsync(attachment => attachment.ResourceId == card.Id, ct);

        return Result<CardDto>.Success(new CardDto(
            card.Id, listContext.BoardId, listContext.WorkspaceId, card.ListId, card.Title, card.DescriptionMd, card.LinkedPageId,
            card.Priority?.ToString(), card.Status.ToString(), card.DueDate, card.StartDate,
            card.CompletedAt, card.Cover, card.Position, members, labels, checklists,
            commentCount, attachmentCount,
            card.FieldValues,
            card.CreatedAt, card.UpdatedAt
        ));
    }
}
