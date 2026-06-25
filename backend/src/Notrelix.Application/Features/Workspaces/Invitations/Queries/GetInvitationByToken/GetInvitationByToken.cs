using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

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

public record GetInvitationByTokenQuery(string Token) : IQuery<Result<WorkspaceInvitationDto>>;

public class GetInvitationByTokenQueryHandler : IRequestHandler<GetInvitationByTokenQuery, Result<WorkspaceInvitationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetInvitationByTokenQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<WorkspaceInvitationDto>> Handle(GetInvitationByTokenQuery request, CancellationToken ct)
    {
        var tokenHash = InvitationTokenHash.Create(request.Token);
        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash, ct);

        if (invitation == null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.Token);

        var workspace = await _context.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == invitation.WorkspaceId, ct);

        var inviter = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == invitation.InvitedBy, ct);

        var inviterName = inviter?.Name ?? "Ai đó";
        if (string.IsNullOrWhiteSpace(inviterName))
        {
            inviterName = inviter?.Email?.Value ?? "Người dùng Workspace";
        }

        var now = _dateTimeProvider.UtcNow;
        var dto = new WorkspaceInvitationDto(
            invitation.Id,
            workspace?.Name ?? "Unknown",
            inviterName,
            invitation.Email,
            invitation.Role.ToString(),
            now >= invitation.ExpiresAt,
            invitation.Status == WorkspaceInvitationStatus.Accepted
        );

        return Result<WorkspaceInvitationDto>.Success(dto);
    }
}
