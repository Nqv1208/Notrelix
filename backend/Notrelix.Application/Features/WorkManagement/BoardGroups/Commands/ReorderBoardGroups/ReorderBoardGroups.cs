using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ReorderBoardGroups;

public record ReorderBoardGroupsCommand(Guid BoardId, List<ReorderItem> Items) : ICommand<Result>, ITransactionalRequest;

public class ReorderBoardGroupsCommandHandler : IRequestHandler<ReorderBoardGroupsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReorderBoardGroupsCommandHandler(
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

    public async Task<Result> Handle(ReorderBoardGroupsCommand request, CancellationToken ct)
    {
        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var itemIds = request.Items.Select(item => item.Id).ToHashSet();
        var lists = await _context.BoardGroups
            .Where(list => itemIds.Contains(list.Id))
            .ToListAsync(ct);

        if (lists.Count != itemIds.Count)
            throw new NotFoundException(nameof(BoardGroup), string.Join(",", itemIds));

        if (lists.Any(list => list.BoardId != request.BoardId))
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleViolationException("ListBoardMismatch", "All reordered groups must belong to the requested board.");

        var now = _dateTimeProvider.UtcNow;
        var positionsById = request.Items.ToDictionary(item => item.Id, item => item.NewPosition);
        foreach (var list in lists)
        {
            list.UpdatePosition(FractionalIndex.Create(positionsById[list.Id].ToString("F0")), _currentUser.UserId, now);
        }

        return Result.Success();
    }
}
