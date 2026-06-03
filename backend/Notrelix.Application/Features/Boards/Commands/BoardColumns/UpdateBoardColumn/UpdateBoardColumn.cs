using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns.UpdateBoardColumn;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns;
using global::Notrelix.Application.Features.Boards.Commands.Boards.UpdateBoard;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.BoardColumns.UpdateBoardColumn;

public record UpdateBoardColumnCommand(Guid BoardId, Guid ColumnId, string? Name, string? FieldType, string? Settings, bool? IsHidden) : IRequest<Result>;

public class UpdateBoardColumnCommandHandler : IRequestHandler<UpdateBoardColumnCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateBoardColumnCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateBoardColumnCommand request, CancellationToken ct)
    {
        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var column = await _context.BoardColumns
            .FirstOrDefaultAsync(item => item.Id == request.ColumnId && item.BoardId == request.BoardId, ct);
        if (column is null) throw new NotFoundException(nameof(BoardColumn), request.ColumnId);

        column.Update(
            request.Name ?? column.Name,
            request.FieldType ?? column.FieldType,
            request.Settings ?? column.Settings);

        if (request.IsHidden == true) column.Hide();
        if (request.IsHidden == false) column.Show();

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
