using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.Security.Commands.CompleteStepUpPassword;

/// <summary>
/// Completes step-up verification with the current password and issues a
/// single-use proof bound to the current user, session and purpose.
/// </summary>
public sealed record CompleteStepUpPasswordCommand
    : ICommand<Result<StepUpProofResult>>,
      IAuthenticatedRequest,
      IGlobalRequest,
      IWriteRequest
{
    public required StepUpPurpose Purpose { get; init; }
    public required string Password { get; init; }
}

public sealed class CompleteStepUpPasswordCommandHandler
    : IRequestHandler<CompleteStepUpPasswordCommand, Result<StepUpProofResult>>
{
    private readonly ISecurityStepUpService _stepUpService;
    private readonly ICurrentRequestContext _currentUser;

    public CompleteStepUpPasswordCommandHandler(
        ISecurityStepUpService stepUpService,
        ICurrentRequestContext currentUser)
    {
        _stepUpService = stepUpService;
        _currentUser = currentUser;
    }

    public async Task<Result<StepUpProofResult>> Handle(
        CompleteStepUpPasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.SessionId is not { } sessionId)
        {
            return Result<StepUpProofResult>.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        return await _stepUpService.CompletePasswordAsync(
            _currentUser.UserId,
            sessionId,
            request.Purpose,
            request.Password,
            cancellationToken);
    }
}