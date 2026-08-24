using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
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

public record GetInvitationByTokenQuery(string Token)
    : IQuery<Result<WorkspaceInvitationDto>>, IAnonymousTokenScopedRequest, IReadRequest
{
    TokenPurpose ITokenScopedRequest.TokenPurpose => TokenPurpose.WorkspaceInvitation;
}

public class GetInvitationByTokenQueryHandler : IRequestHandler<GetInvitationByTokenQuery, Result<WorkspaceInvitationDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOneTimeTokenService _oneTimeTokenService;

    public GetInvitationByTokenQueryHandler(
        IWorkspaceDbContext context,
        IActorLookupService actorLookup,
        IDateTimeProvider dateTimeProvider,
        IOneTimeTokenService oneTimeTokenService)
    {
        _context = context;
        _actorLookup = actorLookup;
        _dateTimeProvider = dateTimeProvider;
        _oneTimeTokenService = oneTimeTokenService;
    }

    public async Task<Result<WorkspaceInvitationDto>> Handle(GetInvitationByTokenQuery request, CancellationToken ct)
    {
        ParsedOneTimeToken presentedHash;
        try
        {
            presentedHash = _oneTimeTokenService.ParseAndHash(
                request.Token,
                TokenPurpose.WorkspaceInvitation);
        }
        catch (InvalidOneTimeTokenException)
        {
            return Result<WorkspaceInvitationDto>.Failure(
                "Invalid or expired invitation token.");
        }

        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(
                i => i.Token.Value == presentedHash.TokenHash
                    && i.HashVersion == presentedHash.HashVersion,
                ct);

        if (invitation == null)
            throw new NotFoundException(nameof(WorkspaceInvitation), "Invalid invitation token.");

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
