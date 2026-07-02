using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

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
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetInvitationByTokenQueryHandler(IWorkspaceDbContext context, IActorLookupService actorLookup, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _actorLookup = actorLookup;
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

        var inviter = await _actorLookup.FindAsync(invitation.InvitedBy, ct);
        var inviterName = inviter?.Name ?? "Ai đó";
        if (string.IsNullOrWhiteSpace(inviterName))
        {
            inviterName = "Người dùng Workspace";
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
