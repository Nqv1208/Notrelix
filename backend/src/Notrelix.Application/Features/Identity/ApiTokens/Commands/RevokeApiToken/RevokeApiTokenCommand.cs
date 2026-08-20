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
      ITransactionalRequest,
      IWorkspaceRequest,
      IAuthenticatedRequest,
      IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class RevokeApiTokenCommandHandler
    : IRequestHandler<RevokeApiTokenCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RevokeApiTokenCommandHandler> _logger;

    public RevokeApiTokenCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<RevokeApiTokenCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeApiTokenCommand request, CancellationToken ct)
    {
        var token = await _context.ApiTokens.SingleOrDefaultAsync(
            t => t.Id == request.TokenId, ct);

        if (token is null || token.WorkspaceId != request.WorkspaceId)
        {
            throw new NotFoundException(nameof(ApiToken), request.TokenId);
        }

        if (token.Status != ApiTokenStatus.Revoked)
        {
            var userId = _currentUser.UserId;
            token.Revoke(userId, _dateTimeProvider.UtcNow);
            _logger.LogInformation(
                "Revoked API token {TokenId} in workspace {WorkspaceId} by user {UserId}",
                token.Id, request.WorkspaceId, userId);
        }

        return Result.Success();
    }
}