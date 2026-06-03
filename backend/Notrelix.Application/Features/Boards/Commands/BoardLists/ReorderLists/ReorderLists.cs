using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.ReorderLists;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Common;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.BoardLists.ReorderLists;

public record ReorderListsCommand(Guid BoardId, List<ReorderItem> Items) : IRequest<Result>;

public class ReorderListsCommandHandler : IRequestHandler<ReorderListsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public ReorderListsCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(ReorderListsCommand request, CancellationToken ct)
    {
        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var itemIds = request.Items.Select(item => item.Id).ToHashSet();
        var lists = await _context.BoardLists
            .Where(list => itemIds.Contains(list.Id))
            .ToListAsync(ct);

        if (lists.Count != itemIds.Count)
            throw new NotFoundException(nameof(BoardList), string.Join(",", itemIds));

        if (lists.Any(list => list.BoardId != request.BoardId))
            throw new BusinessRuleViolationException("ListBoardMismatch", "All reordered groups must belong to the requested board.");

        var positionsById = request.Items.ToDictionary(item => item.Id, item => item.NewPosition);
        foreach (var list in lists)
        {
            list.Move(positionsById[list.Id], _currentUser.UserId);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
