using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.ApiTokens.Abstractions;
using Notrelix.Application.Features.Identity.ApiTokens.DTOs;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;

/// <summary>
/// Issues a new API token for the workspace. Protected by step-up verification
/// (IssueApiToken purpose). The raw secret is returned exactly once.
/// </summary>
public sealed record CreateApiTokenCommand(
    Guid WorkspaceId,
    string Name,
    DateTimeOffset? ExpiresAt,
    string StepUpToken)
    : ICommand<Result<CreatedApiTokenDto>>,
      ITransactionalRequest,
      IWorkspaceRequest,
      IAuthenticatedRequest,
      IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class CreateApiTokenCommandHandler
    : IRequestHandler<CreateApiTokenCommand, Result<CreatedApiTokenDto>>
{
    public const int MaxTokenNameLength = 256;

    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IApiTokenSecretService _secretService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreateApiTokenCommandHandler> _logger;

    public CreateApiTokenCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        ISecurityStepUpService stepUpService,
        IApiTokenSecretService secretService,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreateApiTokenCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _stepUpService = stepUpService;
        _secretService = secretService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<CreatedApiTokenDto>> Handle(
        CreateApiTokenCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var sessionId = _currentUser.SessionId;

        if (sessionId is null)
        {
            return Result<CreatedApiTokenDto>.Failure(new ApplicationError(
                "identity.api-tokens.session-required",
                "API token issuance requires an authenticated session.",
                ApplicationErrorType.PreconditionFailed));
        }

        var stepUp = await _stepUpService.ConsumeAsync(
            request.StepUpToken, userId, sessionId.Value, StepUpPurpose.IssueApiToken, ct);
        if (!stepUp.Succeeded)
        {
            return Result<CreatedApiTokenDto>.Failure(stepUp.TypedErrors);
        }

        var name = request.Name.Trim();
        if (name.Length == 0 || name.Length > MaxTokenNameLength)
        {
            return Result<CreatedApiTokenDto>.Failure(new ApplicationError(
                "identity.api-tokens.invalid-name",
                $"Token name must be between 1 and {MaxTokenNameLength} characters.",
                ApplicationErrorType.Validation,
                nameof(CreateApiTokenCommand.Name)));
        }

        var now = _dateTimeProvider.UtcNow;
        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= now)
        {
            return Result<CreatedApiTokenDto>.Failure(new ApplicationError(
                "identity.api-tokens.invalid-expiration",
                "Token expiration must be in the future.",
                ApplicationErrorType.Validation,
                nameof(CreateApiTokenCommand.ExpiresAt)));
        }

        var secret = _secretService.Generate();
        var token = ApiToken.Create(
            accountId: _currentUser.RequireAccountId(),
            workspaceId: request.WorkspaceId,
            userId: userId,
            name: name,
            tokenHash: secret.TokenHash,
            scopes: null,
            createdBy: userId,
            createdAt: now,
            expiresAt: request.ExpiresAt);

        _context.ApiTokens.Add(token);
        _logger.LogInformation(
            "Issued API token {TokenId} for workspace {WorkspaceId} by user {UserId}",
            token.Id, request.WorkspaceId, userId);

        return Result<CreatedApiTokenDto>.Success(new CreatedApiTokenDto(
            token.Id,
            secret.RawToken,
            token.Name,
            token.ExpiresAt,
            token.CreatedAt));
    }
}