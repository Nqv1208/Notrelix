using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Invitations.Queries.GetInvitationByToken;

public record WorkspaceInvitationDto(
    Guid Id,
    string WorkspaceName,
    string InviterName,
    string Email,
    string Role,
    bool IsExpired,
    bool IsAccepted
);

public record GetInvitationByTokenQuery(string Token) : IRequest<Result<WorkspaceInvitationDto>>;

public class GetInvitationByTokenQueryHandler : IRequestHandler<GetInvitationByTokenQuery, Result<WorkspaceInvitationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInvitationByTokenQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<WorkspaceInvitationDto>> Handle(GetInvitationByTokenQuery request, CancellationToken ct)
    {
        var invitation = await _context.WorkspaceInvitations
            .Include(i => i.Workspace)
            .FirstOrDefaultAsync(i => i.Token == request.Token, ct);

        if (invitation == null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.Token);

        // Lấy tên của người mời
        var inviter = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == invitation.InvitedBy, ct);
        
        var inviterName = inviter != null ? inviter.Name : "Ai đó";
        if (string.IsNullOrWhiteSpace(inviterName))
        {
            inviterName = inviter?.Email?.Value ?? "Người dùng Workspace";
        }

        var dto = new WorkspaceInvitationDto(
            invitation.Id,
            invitation.Workspace.Name,
            inviterName,
            invitation.Email,
            invitation.Role.ToString(),
            invitation.IsExpired,
            invitation.IsAccepted
        );

        return Result<WorkspaceInvitationDto>.Success(dto);
    }
}
