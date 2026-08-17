using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.Security.Queries.GetStepUpRequirement;

/// <summary>
/// Returns the step-up factor the current user must satisfy for a security
/// purpose and, when MFA is enrolled, issues the bound MFA challenge token.
/// </summary>
public sealed record GetStepUpRequirementQuery
    : IQuery<Result<StepUpRequirementResult>>,
      IAuthenticatedRequest,
      IGlobalRequest
{
    public required StepUpPurpose Purpose { get; init; }
}

public sealed class GetStepUpRequirementQueryHandler
    : IRequestHandler<GetStepUpRequirementQuery, Result<StepUpRequirementResult>>
{
    private readonly ISecurityStepUpService _stepUpService;
    private readonly ICurrentRequestContext _currentUser;

    public GetStepUpRequirementQueryHandler(
        ISecurityStepUpService stepUpService,
        ICurrentRequestContext currentUser)
    {
        _stepUpService = stepUpService;
        _currentUser = currentUser;
    }

    public async Task<Result<StepUpRequirementResult>> Handle(
        GetStepUpRequirementQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.SessionId is not { } sessionId)
        {
            return Result<StepUpRequirementResult>.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        return await _stepUpService.GetRequirementAsync(
            _currentUser.UserId, sessionId, request.Purpose, cancellationToken);
    }
}