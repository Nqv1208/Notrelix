using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceInvitations;

public record GetWorkspaceInvitationsQuery(Guid WorkspaceId) : IRequest<Result<List<WorkspaceInvitationDto>>>;

public class GetWorkspaceInvitationsQueryHandler : IRequestHandler<GetWorkspaceInvitationsQuery, Result<List<WorkspaceInvitationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetWorkspaceInvitationsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<WorkspaceInvitationDto>>> Handle(GetWorkspaceInvitationsQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == _currentUser.UserId, ct);

        if (!isMember)
            throw new ForbiddenException("Bạn không phải thành viên của workspace này.");

        var invitations = await _context.WorkspaceInvitations
            .AsNoTracking()
            .Where(invitation => invitation.WorkspaceId == request.WorkspaceId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new WorkspaceInvitationDto(
                invitation.Id,
                invitation.Email,
                invitation.Role.ToString(),
                invitation.ExpiresAt,
                invitation.IsAccepted,
                invitation.CreatedAt
            ))
            .ToListAsync(ct);

        return Result<List<WorkspaceInvitationDto>>.Success(invitations);
    }
}
