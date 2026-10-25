using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Identity.Public.InvitedRegistration;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationWithRegistration;

/// <summary>
/// What the invitee sees after pressing accept without an account. <see cref="Session"/> is the session
/// issued for the new identity (delivered as httpOnly cookies by the API, never in a body); it is null
/// on a repeat press, which answers the same success without signing anyone in.
/// </summary>
public sealed record AcceptInvitationWithRegistrationDto(
    AcceptInvitationResultDto Acceptance,
    bool AlreadyAccepted,
    InvitedUserRegistration? Session);

/// <summary>
/// A person with no account accepts an invitation by choosing a name and a password: one use case, one
/// transaction (WSP-026). The one-time invitation token is the authority; it proves control of the
/// invited address, so the new identity starts with a confirmed email. The identity, its personal
/// Account, the Workspace membership, the guest resource entry and any initial entries commit together
/// or not at all. A repeat press on an accepted invitation answers the same success (no second
/// membership, no second session). An address that already has an identity must sign in instead.
/// </summary>
public sealed record AcceptInvitationWithRegistrationCommand(string Token, string Name, string Password)
    : ICommand<Result<AcceptInvitationWithRegistrationDto>>,
      IAnonymousTokenScopedRequest,
      IWriteRequest
{
    TokenPurpose ITokenScopedRequest.TokenPurpose => TokenPurpose.WorkspaceInvitation;
}

public sealed class AcceptInvitationWithRegistrationCommandHandler
    : IRequestHandler<AcceptInvitationWithRegistrationCommand, Result<AcceptInvitationWithRegistrationDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly InvitationTokenScope _tokenScope;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IInvitedUserRegistrationActions _registration;
    private readonly InvitationAcceptanceService _acceptance;

    public AcceptInvitationWithRegistrationCommandHandler(
        IWorkspaceDbContext context,
        InvitationTokenScope tokenScope,
        IOneTimeTokenService oneTimeTokenService,
        IDateTimeProvider dateTimeProvider,
        IInvitedUserRegistrationActions registration,
        InvitationAcceptanceService acceptance)
    {
        _context = context;
        _tokenScope = tokenScope;
        _oneTimeTokenService = oneTimeTokenService;
        _dateTimeProvider = dateTimeProvider;
        _registration = registration;
        _acceptance = acceptance;
    }

    public async Task<Result<AcceptInvitationWithRegistrationDto>> Handle(
        AcceptInvitationWithRegistrationCommand request, CancellationToken ct)
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

        var workspace = await _context.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == invitation.WorkspaceId, ct);
        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), invitation.WorkspaceId);

        var now = _dateTimeProvider.UtcNow;

        if (invitation.Status == WorkspaceInvitationStatus.Accepted)
        {
            return Result<AcceptInvitationWithRegistrationDto>.Success(new(
                new AcceptInvitationResultDto(
                    workspace.Slug,
                    invitation.WorkspaceId,
                    invitation.ResourceGrant?.ResourceKind,
                    invitation.ResourceGrant?.ResourceId),
                AlreadyAccepted: true,
                Session: null));
        }

        if (invitation.Status == WorkspaceInvitationStatus.Pending && now >= invitation.ExpiresAt)
            return Result<AcceptInvitationWithRegistrationDto>.Failure("This invitation has expired.");
        if (invitation.Status != WorkspaceInvitationStatus.Pending)
            return Result<AcceptInvitationWithRegistrationDto>.Failure("This invitation is no longer valid.");

        var registration = await _registration.RegisterAsync(
            invitation.Email, request.Name, request.Password, now, ct);
        if (registration is null)
        {
            // An identity already exists for the address: nothing was created; the invitee signs in.
            return Result<AcceptInvitationWithRegistrationDto>.Failure(new ApplicationError(
                "workspaces.invitation.account-exists",
                "An account already exists for this address. Sign in to accept the invitation.",
                ApplicationErrorType.Conflict));
        }

        await _tokenScope.AdoptAsync(invitation, registration.UserId, ct);

        var accepted = await _acceptance.AcceptAsync(invitation, registration.UserId, invitation.Email, now, ct);
        if (!accepted.Succeeded || accepted.Data is null)
        {
            // The identity was already added to the unit of work: only an exception rolls it back.
            throw new Common.Exceptions.BusinessRuleException(
                accepted.Errors.FirstOrDefault() ?? "The invitation cannot be accepted.");
        }

        return Result<AcceptInvitationWithRegistrationDto>.Success(
            new(accepted.Data, AlreadyAccepted: false, registration));
    }
}

public sealed class AcceptInvitationWithRegistrationCommandValidator
    : AbstractValidator<AcceptInvitationWithRegistrationCommand>
{
    public AcceptInvitationWithRegistrationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(PasswordPolicy.MinimumLength)
                .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters");
    }
}
