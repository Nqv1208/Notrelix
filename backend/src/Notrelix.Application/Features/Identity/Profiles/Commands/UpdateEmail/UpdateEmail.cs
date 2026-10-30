using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Registration.Ports;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Verification.Abstractions;

namespace Notrelix.Application.Features.Identity.Profiles.Commands.UpdateEmail;

/// <summary>
/// <paramref name="StepUpToken"/> is the single-use proof of purpose ChangeSecurityIdentity: the sign-in
/// email is a recovery identifier, so a session alone may not change it.
/// </summary>
public sealed record UpdateEmailCommand(string Email, string StepUpToken = "")
    : ICommand<Result<UpdateEmailResultDto>>,
      IAuthenticatedRequest,
      IWriteRequest,
      IGlobalRequest;

public sealed record UpdateEmailResultDto(
    bool EmailConfirmed,
    bool SessionRefreshRequired);

public sealed class UpdateEmailCommandHandler
    : IRequestHandler<UpdateEmailCommand, Result<UpdateEmailResultDto>>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IEmailVerificationTokenIssuer _tokenIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IEmailRegistrationProbe? _registrationProbe;

    public UpdateEmailCommandHandler(
        IIdentityDbContext identityContext,
        ICurrentRequestContext requestContext,
        IEmailVerificationTokenIssuer tokenIssuer,
        IDateTimeProvider dateTimeProvider,
        ISecurityStepUpService stepUpService,
        IEmailRegistrationProbe? registrationProbe = null)
    {
        _stepUpService = stepUpService;
        _registrationProbe = registrationProbe;
        _identityContext = identityContext;
        _requestContext = requestContext;
        _tokenIssuer = tokenIssuer;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UpdateEmailResultDto>> Handle(
        UpdateEmailCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _identityContext.Users
            .FirstOrDefaultAsync(
                x => x.Id == _requestContext.UserId,
                cancellationToken);

        if (user is null)
            return Result<UpdateEmailResultDto>.Failure(SessionErrors.SessionUserMissing);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Proof first: a session without it learns nothing, not even whether an address is taken.
        if (normalizedEmail != user.NormalizedEmail)
        {
            var stepUp = await ConsumeStepUpAsync(user.Id, request.StepUpToken, cancellationToken);
            if (!stepUp.Succeeded)
                return Result<UpdateEmailResultDto>.Failure(stepUp.TypedErrors);
        }

        // Another person's row is not visible to this user's own-row access: the boolean probe answers for
        // every address. The caller's own current address never blocks (it is not "in use by someone else").
        var emailInUse = normalizedEmail != user.NormalizedEmail
            && (_registrationProbe is not null
                ? await _registrationProbe.IsRegisteredAsync(normalizedEmail, cancellationToken)
                : await _identityContext.Users
                    .AnyAsync(
                        x => x.NormalizedEmail == normalizedEmail && x.Id != user.Id,
                        cancellationToken));

        if (emailInUse)
            return Result<UpdateEmailResultDto>.Failure("Email is already in use.");

        var now = _dateTimeProvider.UtcNow;
        user.UpdateEmail(normalizedEmail, _requestContext.UserId, now);
        await _tokenIssuer.IssueAsync(user, user.Id, now, cancellationToken);

        return Result<UpdateEmailResultDto>.Success(
            new UpdateEmailResultDto(
                EmailConfirmed: false,
                SessionRefreshRequired: true));
    }

    private async Task<Result> ConsumeStepUpAsync(Guid userId, string stepUpToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stepUpToken) || _requestContext.SessionId is not { } sessionId)
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        return await _stepUpService.ConsumeAsync(
            stepUpToken, userId, sessionId, StepUpPurpose.ChangeSecurityIdentity, cancellationToken);
    }
}
