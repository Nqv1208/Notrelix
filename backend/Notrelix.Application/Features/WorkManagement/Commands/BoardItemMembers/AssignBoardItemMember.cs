using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

using global::Notrelix.Application.Common.Security;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record AssignCardMemberCommand(
    Guid WorkspaceId,
    Guid BoardItemId,
    Guid UserId) : IRequest<Result>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.BoardItem;
    Guid IAuthorizeableRequest.ResourceId => BoardItemId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.AssignItem;
}

public class AssignCardMemberCommandHandler : IRequestHandler<AssignCardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AssignCardMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AssignCardMemberCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .Include(c => c.Group)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var isMemberOfWorkspace = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId, ct);

        if (!isMemberOfWorkspace)
            throw new ForbiddenException("Chỉ có thể assign thành viên thuộc cùng workspace.");

        card.AssignMember(request.UserId, _currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
