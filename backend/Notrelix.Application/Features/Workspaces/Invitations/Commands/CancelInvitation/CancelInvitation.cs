using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.CancelInvitation;

public record CancelInvitationCommand(
    Guid WorkspaceId,
    Guid InvitationId
) : IRequest<Result>;

public class CancelInvitationCommandHandler : IRequestHandler<CancelInvitationCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public CancelInvitationCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(CancelInvitationCommand request, CancellationToken ct)
    {
        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId && i.WorkspaceId == request.WorkspaceId, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        _context.WorkspaceInvitations.Remove(invitation);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
