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

public record ArchiveBoardItemCommand(Guid BoardItemId) : IRequest<Result>;

public class ArchiveBoardItemCommandHandler : IRequestHandler<ArchiveBoardItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public ArchiveBoardItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(ArchiveBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);
        await _permissions.EnsureCanEditBoardAsync(card.Group.BoardId, _currentUser.UserId, ct);
        card.Archive(_currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
