using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Boards.DTOs;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Workspaces;
using BoardEntity = Notrelix.Domain.Entities.Boards.Board;

namespace Notrelix.Application.Features.Boards.Queries;

// ── GetBoard ─────────────────────────────────────────────────
public class GetBoardQueryHandler : IRequestHandler<GetBoardQuery, Result<BoardDto>>
{
    private readonly IApplicationDbContext _context;
    public GetBoardQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<BoardDto>> Handle(GetBoardQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var memberCount = await _context.BoardMembers.CountAsync(m => m.BoardId == board.Id, ct);
        var listCount = await _context.BoardLists.CountAsync(l => l.BoardId == board.Id && !l.IsArchived, ct);

        return Result<BoardDto>.Success(new BoardDto(
            board.Id, board.WorkspaceId, board.Title, board.Description,
            board.Background, board.Visibility.ToString(), board.IsArchived,
            memberCount, listCount, board.CreatedAt
        ));
    }
}

// ── GetBoardsBySlug ──────────────────────────────────────────
public class GetBoardsBySlugQueryHandler : IRequestHandler<GetBoardsBySlugQuery, Result<List<BoardDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetBoardsBySlugQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<BoardDto>>> Handle(GetBoardsBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);
        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        var boards = await _context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == workspace.Id && !b.IsArchived)
            .Select(b => new BoardDto(
                b.Id, b.WorkspaceId, b.Title, b.Description,
                b.Background, b.Visibility.ToString(), b.IsArchived,
                _context.BoardMembers.Count(m => m.BoardId == b.Id),
                _context.BoardLists.Count(l => l.BoardId == b.Id && !l.IsArchived),
                b.CreatedAt
            )).ToListAsync(ct);

        return Result<List<BoardDto>>.Success(boards);
    }
}

// ── GetBoardMembers ──────────────────────────────────────────
public class GetBoardMembersQueryHandler : IRequestHandler<GetBoardMembersQuery, Result<List<BoardMemberDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetBoardMembersQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<BoardMemberDto>>> Handle(GetBoardMembersQuery request, CancellationToken ct)
    {
        var members = await _context.BoardMembers.AsNoTracking()
            .Where(m => m.BoardId == request.BoardId)
            .Join(_context.Users.AsNoTracking(), m => m.UserId, u => u.Id,
                (m, u) => new BoardMemberDto(m.UserId, u.Name, u.AvatarUrl, m.Role.ToString(), m.JoinedAt))
            .ToListAsync(ct);

        return Result<List<BoardMemberDto>>.Success(members);
    }
}

// ── GetBoardView ─────────────────────────────────────────────
public class GetBoardViewQueryHandler : IRequestHandler<GetBoardViewQuery, Result<object>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetBoardViewQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<object>> Handle(GetBoardViewQuery request, CancellationToken ct)
    {
        var view = await _context.BoardViews.AsNoTracking()
            .FirstOrDefaultAsync(v => v.BoardId == request.BoardId && v.UserId == _currentUser.UserId, ct);

        if (view is null)
            return Result<object>.Success(new { viewMode = "Kanban", filters = "{}" });

        return Result<object>.Success(new { viewMode = view.ViewMode.ToString(), filters = view.Filters });
    }
}

// ── GetLabels ────────────────────────────────────────────────
public class GetLabelsQueryHandler : IRequestHandler<GetLabelsQuery, Result<List<CardLabelDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetLabelsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<CardLabelDto>>> Handle(GetLabelsQuery request, CancellationToken ct)
    {
        var labels = await _context.Labels.AsNoTracking()
            .Where(l => l.BoardId == request.BoardId)
            .Select(l => new CardLabelDto(l.Id, l.Name, l.Color))
            .ToListAsync(ct);

        return Result<List<CardLabelDto>>.Success(labels);
    }
}

// ── GetChecklists ────────────────────────────────────────────
public class GetChecklistsQueryHandler : IRequestHandler<GetChecklistsQuery, Result<List<ChecklistDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetChecklistsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<ChecklistDto>>> Handle(GetChecklistsQuery request, CancellationToken ct)
    {
        var checklists = await _context.Checklists.AsNoTracking()
            .Where(c => c.CardId == request.CardId)
            .OrderBy(c => c.Position)
            .Select(c => new ChecklistDto(
                c.Id, c.Title, c.Position,
                _context.ChecklistItems.AsNoTracking()
                    .Where(i => i.ChecklistId == c.Id)
                    .OrderBy(i => i.Position)
                    .Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsChecked, i.DueDate, i.AssigneeId, i.Position))
                    .ToList()
            )).ToListAsync(ct);

        return Result<List<ChecklistDto>>.Success(checklists);
    }
}

// ── GetCard ──────────────────────────────────────────────────
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

        return Result<CardDto>.Success(new CardDto(
            card.Id, card.ListId, card.Title, card.DescriptionMd, card.LinkedPageId,
            card.Priority?.ToString(), card.Status.ToString(), card.DueDate, card.StartDate,
            card.CompletedAt, card.Cover, card.Position, members, labels, checklists,
            card.CreatedAt, card.UpdatedAt
        ));
    }
}
