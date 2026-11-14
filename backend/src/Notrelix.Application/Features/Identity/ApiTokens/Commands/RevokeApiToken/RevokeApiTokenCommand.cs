using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.ApiTokens.Commands.RevokeApiToken;

/// <summary>
/// Revokes an API token immediately. Revocation is authoritative: a revoked
/// token can never authenticate again (verification checks persisted status).
/// </summary>
public sealed record RevokeApiTokenCommand(
    Guid WorkspaceId,
    Guid TokenId)
    : ICommand<Result>,
      IWriteRequest,
      IWorkspaceRequest,
      IAuthenticatedRequest,
      IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageApiTokens;
    public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class RevokeApiTokenCommandHandler
    : IRequestHandler<RevokeApiTokenCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RevokeApiTokenCommandHandler> _logger;
    private readonly IAuditService _auditService;

    public RevokeApiTokenCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<RevokeApiTokenCommandHandler> logger,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<Result> Handle(RevokeApiTokenCommand request, CancellationToken ct)
    {
        var token = await _context.ApiTokens.SingleOrDefaultAsync(
            t => t.Id == request.TokenId, ct);

        if (token is null || token.WorkspaceId != request.WorkspaceId)
        {
            throw new NotFoundException(nameof(ApiToken), request.TokenId);
        }

        // Already revoked is a no-op: nothing changes, so nothing is audited.
        if (token.Status != ApiTokenStatus.Revoked)
        {
            var userId = _currentUser.UserId;
            var before = token.Status;
            token.Revoke(userId, _dateTimeProvider.UtcNow);
            _logger.LogInformation(
                "Revoked API token {TokenId} in workspace {WorkspaceId} by user {UserId}",
                token.Id, request.WorkspaceId, userId);

            await _auditService.RecordAsync(
                request.WorkspaceId,
                userId,
                "RevokeApiToken",
                ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), request.WorkspaceId, request.WorkspaceId),
                AuditMetadata.Create(),
                AuditSeverity.Info,
                cancellationToken: ct,
                change: new AuditChange(
                    null,
                    null,
                    new { tokenId = token.Id, status = before.ToString() },
                    new { tokenId = token.Id, status = token.Status.ToString() }));
        }

        return Result.Success();
    }
}