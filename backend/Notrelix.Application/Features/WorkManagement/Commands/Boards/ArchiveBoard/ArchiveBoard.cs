using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards.ArchiveBoard;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.ArchiveBoard;

public record ArchiveBoardCommand(Guid BoardId) : IRequest<Result>;

public class ArchiveBoardCommandHandler : IRequestHandler<ArchiveBoardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveBoardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);
        board.Archive(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
