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
      IWriteRequest,
      IWorkspaceRequest,
      IAuthenticatedRequest,
      IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageApiTokens;
    public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class CreateApiTokenCommandHandler
    : IRequestHandler<CreateApiTokenCommand, Result<CreatedApiTokenDto>>
{
    public const int MaxTokenNameLength = 256;

    /// <summary>Default maximum lifetime of an API token (Lead decision L7); a workspace may only shorten it (UC-GOV-H08).</summary>
    public const int MaxTokenLifetimeDays = 365;

    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IApiTokenSecretService _secretService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreateApiTokenCommandHandler> _logger;
    private readonly IAuditService _auditService;

    public CreateApiTokenCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        ISecurityStepUpService stepUpService,
        IApiTokenSecretService secretService,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreateApiTokenCommandHandler> logger,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _stepUpService = stepUpService;
        _secretService = secretService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _auditService = auditService;
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
        if (request.ExpiresAt is null)
        {
            return Result<CreatedApiTokenDto>.Failure(new ApplicationError(
                "identity.api-tokens.expiration-required",
                "An API token needs an expiration date.",
                ApplicationErrorType.Validation,
                nameof(CreateApiTokenCommand.ExpiresAt)));
        }

        if (request.ExpiresAt.Value <= now)
        {
            return Result<CreatedApiTokenDto>.Failure(new ApplicationError(
                "identity.api-tokens.invalid-expiration",
                "Token expiration must be in the future.",
                ApplicationErrorType.Validation,
                nameof(CreateApiTokenCommand.ExpiresAt)));
        }

        if (request.ExpiresAt.Value > now.AddDays(MaxTokenLifetimeDays))
        {
            return Result<CreatedApiTokenDto>.Failure(new ApplicationError(
                "identity.api-tokens.lifetime-exceeded",
                $"An API token cannot live longer than {MaxTokenLifetimeDays} days.",
                ApplicationErrorType.Validation,
                nameof(CreateApiTokenCommand.ExpiresAt)));
        }

        var stepUp = await _stepUpService.ConsumeAsync(
            request.StepUpToken, userId, sessionId.Value, StepUpPurpose.IssueApiToken, ct);
        if (!stepUp.Succeeded)
        {
            return Result<CreatedApiTokenDto>.Failure(stepUp.TypedErrors);
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

        // The audit row carries the token's id, name and expiry, never the secret or its digest.
        await _auditService.RecordAsync(
            request.WorkspaceId,
            userId,
            "CreateApiToken",
            ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), request.WorkspaceId, request.WorkspaceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: ct,
            change: new AuditChange(
                null,
                null,
                null,
                new { tokenId = token.Id, name = token.Name, expiresAt = token.ExpiresAt }));

        return Result<CreatedApiTokenDto>.Success(new CreatedApiTokenDto(
            token.Id,
            secret.RawToken,
            token.Name,
            token.ExpiresAt,
            token.CreatedAt));
    }
}