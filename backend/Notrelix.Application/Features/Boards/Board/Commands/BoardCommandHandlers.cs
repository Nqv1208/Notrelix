using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Boards.DTOs;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
using BoardEntity = Notrelix.Domain.Entities.Boards.Board;

namespace Notrelix.Application.Features.Boards.Commands;

// ── UpdateBoard ──────────────────────────────────────────────
public class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateBoardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        if (request.Title is not null) board.UpdateTitle(request.Title);
        if (request.Description is not null) board.UpdateDescription(request.Description);
        if (request.Background is not null) board.UpdateBackground(request.Background);
        if (request.Visibility is not null)
        {
            var visibility = Enum.Parse<BoardVisibility>(request.Visibility, ignoreCase: true);
            board.UpdateVisibility(visibility);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── ArchiveBoard ─────────────────────────────────────────────
public class ArchiveBoardCommandHandler : IRequestHandler<ArchiveBoardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public ArchiveBoardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(ArchiveBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        board.Archive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── UnarchiveBoard ───────────────────────────────────────────
public class UnarchiveBoardCommandHandler : IRequestHandler<UnarchiveBoardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UnarchiveBoardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UnarchiveBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        board.Unarchive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── CreateBoardBySlug ────────────────────────────────────────
public class CreateBoardBySlugCommandHandler : IRequestHandler<CreateBoardBySlugCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateBoardBySlugCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateBoardBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        var visibility = request.Visibility is not null
            ? Enum.Parse<BoardVisibility>(request.Visibility, ignoreCase: true)
            : BoardVisibility.Workspace;

        var board = BoardEntity.Create(workspace.Id, _currentUser.UserId, request.Title, request.Description ?? "", visibility);

        if (request.Background is not null) board.UpdateBackground(request.Background);

        _context.Boards.Add(board);
        _context.BoardColumns.AddRange(BoardColumn.CreateDefaults(board.Id));
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(board.Id);
    }
}

// ── AddBoardMember ───────────────────────────────────────────
public class AddBoardMemberCommandHandler : IRequestHandler<AddBoardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public AddBoardMemberCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(AddBoardMemberCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);

        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var role = request.Role is not null
            ? Enum.Parse<BoardRole>(request.Role, ignoreCase: true)
            : BoardRole.Member;

        board.AddMember(request.UserId, role);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── RemoveBoardMember ────────────────────────────────────────
public class RemoveBoardMemberCommandHandler : IRequestHandler<RemoveBoardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public RemoveBoardMemberCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(RemoveBoardMemberCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);

        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        board.RemoveMember(request.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── SaveBoardView ────────────────────────────────────────────
public class SaveBoardViewCommandHandler : IRequestHandler<SaveBoardViewCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public SaveBoardViewCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SaveBoardViewCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.BoardId == request.BoardId && v.UserId == _currentUser.UserId, ct);

        var viewMode = Enum.Parse<ViewMode>(request.ViewMode, ignoreCase: true);

        if (view is not null)
        {
            view.UpdateViewMode(viewMode);
            view.UpdateFilters(request.Filters ?? "{}");
        }
        else
        {
            view = BoardView.Create(request.BoardId, _currentUser.UserId, viewMode);
            view.UpdateFilters(request.Filters ?? "{}");
            _context.BoardViews.Add(view);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
