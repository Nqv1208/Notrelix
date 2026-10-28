using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Queries.GetInvitationByToken;

/// <summary>What an invitation link can still do.</summary>
public static class InvitationPreviewStates
{
    public const string Valid = "valid";
    public const string Expired = "expired";

    /// <summary>Accepted, declined or revoked: the link cannot be used again.</summary>
    public const string Used = "used";
}

/// <summary>
/// What the holder of an invitation token may read before signing in. Nothing identifies the invited
/// person unless the invitation can still be used: <see cref="Email"/> and <see cref="HasAccount"/>
/// are withheld (null / false) for an expired or used link. The inviter shows by display name only.
/// </summary>
public record InvitationPreviewDto(
    string State,
    string InviterName,
    string WorkspaceName,
    string Role,
    string? ResourceKind,
    string? ResourceTitle,
    string? ResourceLevel,
    DateOnly ExpiresOn,
    string? Email,
    bool HasAccount
);

public record GetInvitationByTokenQuery(string Token)
    : IQuery<Result<InvitationPreviewDto>>, IAnonymousTokenScopedRequest, IReadRequest
{
    TokenPurpose ITokenScopedRequest.TokenPurpose => TokenPurpose.WorkspaceInvitation;
}

public class GetInvitationByTokenQueryHandler : IRequestHandler<GetInvitationByTokenQuery, Result<InvitationPreviewDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly InvitationTokenScope _tokenScope;
    private readonly IActorLookupService _actorLookup;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly IGuestResourceInvitationFacts _resourceFacts;
    private readonly IIdentityUserFacts _identityUserFacts;

    public GetInvitationByTokenQueryHandler(
        IWorkspaceDbContext context,
        InvitationTokenScope tokenScope,
        IActorLookupService actorLookup,
        IDateTimeProvider dateTimeProvider,
        IOneTimeTokenService oneTimeTokenService,
        IGuestResourceInvitationFacts resourceFacts,
        IIdentityUserFacts identityUserFacts)
    {
        _context = context;
        _tokenScope = tokenScope;
        _actorLookup = actorLookup;
        _dateTimeProvider = dateTimeProvider;
        _oneTimeTokenService = oneTimeTokenService;
        _resourceFacts = resourceFacts;
        _identityUserFacts = identityUserFacts;
    }

    public async Task<Result<InvitationPreviewDto>> Handle(GetInvitationByTokenQuery request, CancellationToken ct)
    {
        // A malformed token and an unknown token answer the same way.
        ParsedOneTimeToken presentedHash;
        try
        {
            presentedHash = _oneTimeTokenService.ParseAndHash(
                request.Token,
                TokenPurpose.WorkspaceInvitation);
        }
        catch (InvalidOneTimeTokenException)
        {
            throw new NotFoundException(nameof(WorkspaceInvitation), "Invalid invitation token.");
        }

        var invitation = await _tokenScope.FindAsync(presentedHash, track: false, ct);

        if (invitation == null)
            throw new NotFoundException(nameof(WorkspaceInvitation), "Invalid invitation token.");

        await _tokenScope.AdoptAsync(invitation, userId: null, ct);

        var workspace = await _context.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == invitation.WorkspaceId, ct);

        var inviter = await _actorLookup.FindAsync(invitation.InvitedBy, ct);
        var inviterName = string.IsNullOrWhiteSpace(inviter?.Name) ? "Người dùng Workspace" : inviter!.Name;

        var now = _dateTimeProvider.UtcNow;
        var state = invitation.Status switch
        {
            WorkspaceInvitationStatus.Pending when now >= invitation.ExpiresAt => InvitationPreviewStates.Expired,
            WorkspaceInvitationStatus.Pending => InvitationPreviewStates.Valid,
            WorkspaceInvitationStatus.Expired => InvitationPreviewStates.Expired,
            _ => InvitationPreviewStates.Used
        };
        var valid = state == InvitationPreviewStates.Valid;

        string? resourceTitle = null;
        if (invitation.ResourceGrant is { } grant)
        {
            resourceTitle = await _resourceFacts.ResolveTitleAsync(
                grant.ResourceKind, grant.ResourceId, invitation.WorkspaceId, ct);
        }

        var hasAccount = valid && await _identityUserFacts.ExistsByEmailAsync(invitation.Email, ct);

        return Result<InvitationPreviewDto>.Success(new InvitationPreviewDto(
            state,
            inviterName,
            workspace?.Name ?? "Unknown",
            invitation.Role.ToString(),
            invitation.ResourceGrant?.ResourceKind,
            resourceTitle,
            invitation.ResourceGrant?.Level,
            DateOnly.FromDateTime(invitation.ExpiresAt.UtcDateTime),
            valid ? invitation.Email : null,
            hasAccount));
    }
}
