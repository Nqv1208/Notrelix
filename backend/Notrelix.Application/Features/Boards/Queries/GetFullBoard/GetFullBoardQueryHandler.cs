using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Boardss.DTOs;
using Notrelix.Domain.Entities.Boardss;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Application.Features.Boardss.Queries.GetFullBoard;

public class GetFullBoardQueryHandler : IRequestHandler<GetFullBoardQuery, FullBoardDto>
{
    private readonly IApplicationDbContext _context;

    public GetFullBoardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FullBoardDto> Handle(GetFullBoardQuery request, CancellationToken cancellationToken)
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
            .Select(l => new ListDto(
                l.Id,
                l.Title,
                l.Position,
                l.IsArchived,
                _context.Cards
                    .AsNoTracking()
                    .Where(c => c.ListId == l.Id && !c.IsDeleted && !c.IsArchived)
                    .OrderBy(c => c.Position)
                    .Select(c => new CardSummaryDto(
                        c.Id,
                        c.Title,
                        c.Priority.ToString(),
                        c.Status.ToString(),
                        c.DueDate,
                        c.Cover,
                        _context.CardMembers.Count(cm => cm.CardId == c.Id),
                        0, // ChecklistProgress
                        0, // ChecklistTotal
                        _context.Comments.Count(comment => comment.ResourceId == c.Id),
                        _context.Attachments.Count(attachment => attachment.ResourceId == c.Id),
                        c.Position
                    )).ToList()
            )).ToListAsync(cancellationToken);

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

        return new FullBoardDto(
            board.Id,
            board.Title,
            board.Description,
            board.Background,
            board.Visibility.ToString(),
            lists,
            members
        );
    }
}
