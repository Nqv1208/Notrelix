using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationCommand(string Token)
    : ICommand<Result<AcceptInvitationResultDto>>,
      IAuthenticatedTokenScopedRequest,
      IRequireVerifiedEmail,
      IWriteRequest
{
    TokenPurpose ITokenScopedRequest.TokenPurpose =>
        TokenPurpose.WorkspaceInvitation;

}

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly IInvitationAcceptanceService _acceptanceService;

    public AcceptInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        ICurrentRequestContext requestContext,
        IOneTimeTokenService oneTimeTokenService,
        IInvitationAcceptanceService acceptanceService)
    {
        _workspaceContext = workspaceContext;
        _requestContext = requestContext;
        _oneTimeTokenService = oneTimeTokenService;
        _acceptanceService = acceptanceService;
    }

    public async Task<Result<AcceptInvitationResultDto>> Handle(
        AcceptInvitationCommand request, CancellationToken ct)
    {
        if (!_requestContext.IsAuthenticated || _requestContext.UserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure(
                "You must be logged in to perform this action.");

        ParsedOneTimeToken presentedHash;
        try
        {
            presentedHash = _oneTimeTokenService.ParseAndHash(
                request.Token,
                TokenPurpose.WorkspaceInvitation);
        }
        catch (InvalidOneTimeTokenException)
        {
            return Result<AcceptInvitationResultDto>.Failure(
                "Invalid or expired invitation token.");
        }

        var invitation = await _workspaceContext.WorkspaceInvitations
            .FirstOrDefaultAsync(
                i => i.Token.Value == presentedHash.TokenHash
                    && i.HashVersion == presentedHash.HashVersion, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), "Invalid invitation token.");

        return await _acceptanceService.AcceptAsync(
            invitation, _requestContext.UserId, ct);
    }
}