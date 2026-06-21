using MediatR;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.CQRS;
using Notrelix.Application.Common.Exceptions;

namespace Notrelix.Application.Common.Behaviors;

public class EntitlementBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEntitlementChecker _entitlementChecker;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EntitlementBehavior<TRequest, TResponse>> _logger;

    public EntitlementBehavior(
        IEntitlementChecker entitlementChecker,
        ICurrentUser currentUser,
        ILogger<EntitlementBehavior<TRequest, TResponse>> logger)
    {
        _entitlementChecker = entitlementChecker;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequireEntitlement entitlementRequest)
        {
            if (_currentUser.UserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("Authentication required.");
            }

            var workspaceId = _currentUser.WorkspaceId;
            if (workspaceId is null || workspaceId.GetValueOrDefault() == Guid.Empty)
            {
                throw new ForbiddenException("No active workspace.");
            }

            var isEntitled = await _entitlementChecker.CheckEntitlementAsync(
                workspaceId.Value,
                entitlementRequest.Feature,
                entitlementRequest.Amount,
                cancellationToken);

            if (!isEntitled)
            {
                throw new ForbiddenException($"Workspace is not entitled for feature '{entitlementRequest.Feature}'.");
            }
        }

        return await next();
    }
}
