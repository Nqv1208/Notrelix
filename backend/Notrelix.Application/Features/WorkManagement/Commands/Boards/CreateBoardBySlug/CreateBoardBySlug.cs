using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards.CreateBoardBySlug;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.CreateBoardBySlug;

public record CreateBoardBySlugCommand(string Slug, string Title, string? Description, string? Background, string? Visibility) : IRequest<Result<Guid>>;

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
        _context.BoardFields.AddRange(BoardField.CreateDefaults(workspace.Id, board.Id));
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(board.Id);
    }
}
