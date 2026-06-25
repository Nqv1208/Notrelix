using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardMemberEntity = global::Notrelix.Domain.WorkManagement.Boards.BoardMember;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;

public record AddBoardMemberCommand(Guid BoardId, Guid UserId, BoardRole? Role) : ICommand<Result>, ITransactionalRequest;

public class AddBoardMemberCommandHandler : IRequestHandler<AddBoardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddBoardMemberCommandHandler(
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

    public async Task<Result> Handle(AddBoardMemberCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);

        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);

        var isWorkspaceMember = await _context.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(member => member.WorkspaceId == board.WorkspaceId && member.UserId == request.UserId, ct);
        if (!isWorkspaceMember)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleViolationException(
                "BoardMemberMustBelongToWorkspace",
                "Board member must belong to the same workspace.");
        }

        var alreadyMember = await _context.BoardMembers
            .AnyAsync(m => m.BoardId == board.Id && m.UserId == request.UserId, ct);
        if (alreadyMember) return Result.Success();

        var role = request.Role ?? BoardRole.Member;

        var member = BoardMemberEntity.Create(board.Id, request.UserId, role, _dateTimeProvider.UtcNow);
        _context.BoardMembers.Add(member);

        return Result.Success();
    }
}
