using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.WorkManagement.Items;
using global::Notrelix.Domain.Workspaces;

using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;

public record AssignBoardItemMemberCommand(
    Guid WorkspaceId,
    Guid BoardItemId,
    Guid UserId) : ICommand<Result>, ITransactionalRequest, IRequirePermission, IWorkspaceRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.AssignItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId, WorkspaceId);
    public RealtimeTopic Topic => new("board", "BoardItem", BoardItemId);
}

public class AssignBoardItemMemberCommandHandler : IRequestHandler<AssignBoardItemMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignBoardItemMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AssignBoardItemMemberCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var isMemberOfWorkspace = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId, ct);

        if (!isMemberOfWorkspace)
            throw new ForbiddenException("Chỉ có thể assign thành viên thuộc cùng workspace.");

        var alreadyAssigned = await _context.BoardItemMembers
            .AnyAsync(m => m.ItemId == card.Id && m.UserId == request.UserId, ct);
        if (alreadyAssigned) return Result.Success();

        var member = BoardItemMember.Create(
            card.WorkspaceId,
            card.BoardId,
            card.Id,
            request.UserId,
            _currentUser.UserId,
            _dateTimeProvider.UtcNow);

        _context.BoardItemMembers.Add(member);
        return Result.Success();
    }
}
