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
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Workspaces.Commands.InviteMember;

public record InviteMemberCommand(
    Guid WorkspaceId,
    string Email,
    string Role
) : IRequest<Result<Guid>>;

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public InviteMemberCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(InviteMemberCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var role = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        var invitation = WorkspaceInvitation.Create(request.WorkspaceId, _currentUser.UserId, request.Email, role);

        _context.WorkspaceInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(invitation.Id);
    }
}
