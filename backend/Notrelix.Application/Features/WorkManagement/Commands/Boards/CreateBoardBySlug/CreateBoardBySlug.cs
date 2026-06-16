using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
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
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.WorkManagement.Fields;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.CreateBoardBySlug;

public record CreateBoardBySlugCommand(string Slug, string Title, string? Description, string? Background, string? Visibility) : IRequest<Result<Guid>>;

public class CreateBoardBySlugCommandHandler : IRequestHandler<CreateBoardBySlugCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardBySlugCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        var createdAt = _dateTimeProvider.UtcNow;
        var visibility = request.Visibility is not null
            ? Enum.Parse<BoardVisibility>(request.Visibility, ignoreCase: true)
            : BoardVisibility.Workspace;

        var board = BoardEntity.Create(workspace.Id, _currentUser.UserId, request.Title, request.Description, createdAt, visibility);

        if (request.Background is not null) board.UpdateBackground(request.Background, _currentUser.UserId, createdAt);

        _context.Boards.Add(board);

        var defaultFields = new[]
        {
            BoardFieldEntity.Create(board.WorkspaceId, board.Id, "Title", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(board.WorkspaceId, board.Id, "Status", FieldType.Status, FieldSettings.Empty(), FractionalIndex.Create("a1"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(board.WorkspaceId, board.Id, "Assignee", FieldType.Person, FieldSettings.Empty(), FractionalIndex.Create("a2"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(board.WorkspaceId, board.Id, "Due Date", FieldType.Date, FieldSettings.Empty(), FractionalIndex.Create("a3"), _currentUser.UserId, createdAt, isSystem: true),
        };
        _context.BoardFields.AddRange(defaultFields);

        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(board.Id);
    }
}
