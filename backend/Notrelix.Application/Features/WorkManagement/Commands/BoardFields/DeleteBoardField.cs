using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record DeleteBoardFieldCommand(Guid BoardId, Guid ColumnId) : IRequest<Result>;

public class DeleteBoardFieldCommandHandler : IRequestHandler<DeleteBoardFieldCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public DeleteBoardFieldCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(DeleteBoardFieldCommand request, CancellationToken ct)
    {
        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var column = await _context.BoardFields
            .FirstOrDefaultAsync(item => item.Id == request.ColumnId && item.BoardId == request.BoardId, ct);
        if (column is null) throw new NotFoundException(nameof(BoardField), request.ColumnId);

        _context.BoardFields.Remove(column);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
