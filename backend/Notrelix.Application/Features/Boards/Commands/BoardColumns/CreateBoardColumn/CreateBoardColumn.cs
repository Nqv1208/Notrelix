using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns.CreateBoardColumn;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.BoardColumns.CreateBoardColumn;

public record CreateBoardColumnCommand(Guid BoardId, string Name, string FieldType, string? Settings, double? Position) : IRequest<Result<Guid>>;

public class CreateBoardColumnCommandHandler : IRequestHandler<CreateBoardColumnCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public CreateBoardColumnCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateBoardColumnCommand request, CancellationToken ct)
    {
        var boardExists = await _context.Boards.AsNoTracking()
            .AnyAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (!boardExists) throw new NotFoundException(nameof(Board), request.BoardId);

        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var position = request.Position ?? await _context.BoardColumns
            .Where(column => column.BoardId == request.BoardId)
            .MaxAsync(column => (double?)column.Position, ct) + 1 ?? 0;

        var column = BoardColumn.Create(
            request.BoardId,
            request.Name,
            request.FieldType,
            request.Settings ?? "{}",
            position);

        _context.BoardColumns.Add(column);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(column.Id);
    }
}
