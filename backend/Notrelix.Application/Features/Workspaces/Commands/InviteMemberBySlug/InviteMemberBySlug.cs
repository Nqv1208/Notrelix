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

namespace Notrelix.Application.Features.Workspaces.Commands.InviteMemberBySlug;

public record InviteMemberBySlugCommand(
    string Slug,
    string Email,
    string Role
) : IRequest<Result<Guid>>;

public class InviteMemberBySlugCommandHandler : IRequestHandler<InviteMemberBySlugCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public InviteMemberBySlugCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(InviteMemberBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var role = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        var invitation = WorkspaceInvitation.Create(workspace.Id, _currentUser.UserId, request.Email, role);

        _context.WorkspaceInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(invitation.Id);
    }
}
