using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitationByToken;

/// <summary>
/// The holder of an invitation link declines it without signing in (a person with no account has no
/// session). Possession of the one-time token is the authority. A second press on an already declined
/// invitation answers the same success; an accepted, revoked or expired invitation cannot be declined.
/// </summary>
public record DeclineInvitationByTokenCommand(string Token)
    : ICommand<Result>, IAnonymousTokenScopedRequest, IWriteRequest
{
    TokenPurpose ITokenScopedRequest.TokenPurpose => TokenPurpose.WorkspaceInvitation;
}

public sealed class DeclineInvitationByTokenCommandHandler : IRequestHandler<DeclineInvitationByTokenCommand, Result>
{
    private readonly InvitationTokenScope _tokenScope;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeclineInvitationByTokenCommandHandler(
        InvitationTokenScope tokenScope,
        IOneTimeTokenService oneTimeTokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _tokenScope = tokenScope;
        _oneTimeTokenService = oneTimeTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeclineInvitationByTokenCommand request, CancellationToken ct)
    {
        ParsedOneTimeToken presentedHash;
        try
        {
            presentedHash = _oneTimeTokenService.ParseAndHash(request.Token, TokenPurpose.WorkspaceInvitation);
        }
        catch (InvalidOneTimeTokenException)
        {
            throw new NotFoundException(nameof(WorkspaceInvitation), "Invalid invitation token.");
        }

        var invitation = await _tokenScope.FindAsync(presentedHash, track: true, ct);
        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), "Invalid invitation token.");

        await _tokenScope.AdoptAsync(invitation, userId: null, ct);

        if (invitation.Status == WorkspaceInvitationStatus.Declined)
            return Result.Success();

        var now = _dateTimeProvider.UtcNow;
        if (invitation.Status == WorkspaceInvitationStatus.Pending && now >= invitation.ExpiresAt)
            return Result.Failure("This invitation has expired.");
        if (invitation.Status != WorkspaceInvitationStatus.Pending)
            return Result.Failure("This invitation is no longer valid.");

        invitation.Decline(null, now);
        return Result.Success();
    }
}

public sealed class DeclineInvitationByTokenCommandValidator : AbstractValidator<DeclineInvitationByTokenCommand>
{
    public DeclineInvitationByTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
