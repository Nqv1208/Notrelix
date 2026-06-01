using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Queries.GetFullBoard;

public record GetFullBoardQuery(Guid BoardId) : IRequest<Result<FullBoardDto>>;

public class GetFullBoardQueryHandler : IRequestHandler<GetFullBoardQuery, Result<FullBoardDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFullBoardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<FullBoardDto>> Handle(GetFullBoardQuery request, CancellationToken cancellationToken)
    {
        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && !b.IsArchived, cancellationToken);

        if (board == null)
            throw new NotFoundException(nameof(Board), request.BoardId);

        var lists = await _context.BoardLists
            .AsNoTracking()
            .Where(l => l.BoardId == request.BoardId && !l.IsArchived)
            .OrderBy(l => l.Position)
            .ToListAsync(cancellationToken);

        var listIds = lists.Select(list => list.Id).ToList();
        var cards = await _context.Cards
            .AsNoTracking()
            .Where(card => listIds.Contains(card.ListId) && !card.IsDeleted && !card.IsArchived)
            .OrderBy(card => card.Position)
            .ToListAsync(cancellationToken);

        var cardIds = cards.Select(card => card.Id).ToList();

        var cardMembers = await _context.CardMembers
            .AsNoTracking()
            .Where(member => cardIds.Contains(member.CardId))
            .Join(_context.Users.AsNoTracking(),
                member => member.UserId,
                user => user.Id,
                (member, user) => new
                {
                    member.CardId,
                    Dto = new CardMemberDto(member.UserId, user.Name, user.Avatar, member.AssignedAt)
                })
            .ToListAsync(cancellationToken);

        var cardLabels = await _context.CardLabels
            .AsNoTracking()
            .Where(cardLabel => cardIds.Contains(cardLabel.CardId))
            .Join(_context.Labels.AsNoTracking(),
                cardLabel => cardLabel.LabelId,
                label => label.Id,
                (cardLabel, label) => new
                {
                    cardLabel.CardId,
                    Dto = new CardLabelDto(label.Id, label.Name, label.Color)
                })
            .ToListAsync(cancellationToken);

        var checklistItems = await _context.ChecklistItems
            .AsNoTracking()
            .Join(_context.Checklists.AsNoTracking().Where(checklist => cardIds.Contains(checklist.CardId)),
                item => item.ChecklistId,
                checklist => checklist.Id,
                (item, checklist) => new { checklist.CardId, item.IsChecked })
            .ToListAsync(cancellationToken);

        var commentCounts = await _context.Comments
            .AsNoTracking()
            .Where(comment => comment.ResourceType == ResourceType.Card && cardIds.Contains(comment.ResourceId))
            .GroupBy(comment => comment.ResourceId)
            .Select(group => new { CardId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CardId, item => item.Count, cancellationToken);

        var attachmentCounts = await _context.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.ResourceType == ResourceType.Card && cardIds.Contains(attachment.ResourceId))
            .GroupBy(attachment => attachment.ResourceId)
            .Select(group => new { CardId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CardId, item => item.Count, cancellationToken);

        var membersByCardId = cardMembers
            .GroupBy(member => member.CardId)
            .ToDictionary(group => group.Key, group => group.Select(member => member.Dto).ToList());
        var labelsByCardId = cardLabels
            .GroupBy(label => label.CardId)
            .ToDictionary(group => group.Key, group => group.Select(label => label.Dto).ToList());
        var checklistStatsByCardId = checklistItems
            .GroupBy(item => item.CardId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Done = group.Count(item => item.IsChecked),
                    Total = group.Count()
                });

        var cardsByListId = cards
            .GroupBy(card => card.ListId)
            .ToDictionary(group => group.Key, group => group
                .OrderBy(card => card.Position)
                .Select(card =>
                {
                    checklistStatsByCardId.TryGetValue(card.Id, out var checklistStats);

                    return new CardSummaryDto(
                        card.Id,
                        card.Title,
                        card.LinkedPageId,
                        card.Priority?.ToString(),
                        card.Status.ToString(),
                        card.DueDate,
                        card.StartDate,
                        card.CompletedAt,
                        card.Cover,
                        membersByCardId.TryGetValue(card.Id, out var cardMemberDtos) ? cardMemberDtos.Count : 0,
                        membersByCardId.TryGetValue(card.Id, out var memberDtos) ? memberDtos : [],
                        labelsByCardId.TryGetValue(card.Id, out var labelDtos) ? labelDtos : [],
                        checklistStats?.Done ?? 0,
                        checklistStats?.Total ?? 0,
                        commentCounts.GetValueOrDefault(card.Id),
                        attachmentCounts.GetValueOrDefault(card.Id),
                        card.Position,
                        card.FieldValues,
                        card.CreatedAt,
                        card.UpdatedAt
                    );
                })
                .ToList());

        var listDtos = lists
            .Select(list => new ListDto(
                list.Id,
                list.Title,
                list.Color,
                list.Position,
                list.IsArchived,
                cardsByListId.GetValueOrDefault(list.Id) ?? []))
            .ToList();

        var columns = await _context.BoardColumns
            .AsNoTracking()
            .Where(column => column.BoardId == request.BoardId)
            .OrderBy(column => column.Position)
            .Select(column => new BoardColumnDto(
                column.Id,
                column.BoardId,
                column.Name,
                column.FieldType,
                column.Settings,
                column.Position,
                column.IsHidden,
                true
            ))
            .ToListAsync(cancellationToken);

        if (!columns.Any(column => IsTitleColumn(column.Name)))
        {
            columns.Insert(0, new BoardColumnDto(
                board.Id,
                board.Id,
                "Task",
                "text",
                """{"system":"title"}""",
                0,
                false,
                true
            ));
        }

        var members = await _context.BoardMembers
            .AsNoTracking()
            .Where(m => m.BoardId == request.BoardId)
            .Join(_context.Users.AsNoTracking(),
                m => m.UserId,
                u => u.Id,
                (m, u) => new BoardMemberDto(
                    m.UserId,
                    u.Name,
                    u.Avatar,
                    m.Role.ToString(),
                    m.JoinedAt
                ))
            .ToListAsync(cancellationToken);

        return Result<FullBoardDto>.Success(new FullBoardDto(
            board.Id,
            board.WorkspaceId,
            board.Title,
            board.Description,
            board.Background,
            board.Visibility.ToString(),
            columns,
            listDtos,
            members
        ));
    }

    private static bool IsTitleColumn(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return normalized is "task" or "title" or "name";
    }
}
