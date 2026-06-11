using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Queries.GetBoardItem;

public record GetBoardItemQuery(Guid BoardItemId) : IRequest<Result<BoardItemDto>>;

public class GetBoardItemQueryHandler : IRequestHandler<GetBoardItemQuery, Result<BoardItemDto>>
{
    private readonly IApplicationDbContext _context;
    public GetBoardItemQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<BoardItemDto>> Handle(GetBoardItemQuery request, CancellationToken ct)
    {
        var card = await _context.BoardItems.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && !c.IsDeleted, ct);

        if (card is null) throw new NotFoundException("BoardItem", request.BoardItemId);

        var members = await _context.BoardItemMembers.AsNoTracking()
            .Where(m => m.BoardItemId == card.Id)
            .Join(_context.Users.AsNoTracking(), m => m.UserId, u => u.Id,
                (m, u) => new BoardItemMemberDto(m.UserId, u.Name, u.AvatarUrl, m.AssignedAt))
            .ToListAsync(ct);

        var labels = await _context.BoardItemLabels.AsNoTracking()
            .Where(cl => cl.BoardItemId == card.Id)
            .Join(_context.Labels.AsNoTracking(), cl => cl.LabelId, l => l.Id,
                (cl, l) => new BoardItemLabelDto(l.Id, l.Name, l.Color))
            .ToListAsync(ct);

        var checklists = await _context.Checklists.AsNoTracking()
            .Where(c => c.BoardItemId == card.Id)
            .OrderBy(c => c.Position)
            .Select(c => new ChecklistDto(
                c.Id, c.Title, c.Position,
                _context.ChecklistItems.AsNoTracking()
                    .Where(i => i.ChecklistId == c.Id)
                    .OrderBy(i => i.Position)
                    .Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsChecked, i.DueDate, i.AssigneeId, i.Position))
                    .ToList()
            )).ToListAsync(ct);

        var listContext = await _context.BoardGroups.AsNoTracking()
            .Where(list => list.Id == card.GroupId)
            .Join(_context.Boards.AsNoTracking(),
                list => list.BoardId,
                board => board.Id,
                (list, board) => new { BoardId = board.Id, board.WorkspaceId })
            .FirstOrDefaultAsync(ct);

        if (listContext is null) throw new NotFoundException("List", card.GroupId);

        var commentCount = await _context.Comments.AsNoTracking()
            .CountAsync(comment => comment.ResourceId == card.Id && !comment.IsDeleted, ct);
        var attachmentCount = await _context.Attachments.AsNoTracking()
            .CountAsync(attachment => attachment.ResourceId == card.Id, ct);

        return Result<BoardItemDto>.Success(new BoardItemDto(
            card.Id, listContext.BoardId, listContext.WorkspaceId, card.GroupId, card.Title, card.DescriptionMd, card.LinkedPageId,
            card.Priority?.ToString(), card.Status.ToString(), card.DueDate, card.StartDate,
            card.CompletedAt, card.Cover, card.Position, members, labels, checklists,
            commentCount, attachmentCount,
            card.ValuesJson,
            card.CreatedAt, card.UpdatedAt
        ));
    }
}
