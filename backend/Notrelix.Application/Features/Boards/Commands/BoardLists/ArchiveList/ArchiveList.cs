using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.ArchiveList;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.BoardLists.ArchiveList;

public record ArchiveListCommand(Guid ListId) : IRequest<Result>;

public class ArchiveListCommandHandler : IRequestHandler<ArchiveListCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public ArchiveListCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(ArchiveListCommand request, CancellationToken ct)
    {
        var list = await _context.BoardLists.FirstOrDefaultAsync(l => l.Id == request.ListId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardList), request.ListId);
        await _permissions.EnsureCanEditBoardAsync(list.BoardId, _currentUser.UserId, ct);
        list.Archive(_currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
