using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Testing.Application.Fakes;

public class FakePermissionService : IPermissionService
{
    private readonly Dictionary<(Guid UserId, Guid WorkspaceId), bool> _authorizationResults = new();
    private bool _defaultResult = true;

    public FakePermissionService WithDefaultResult(bool result)
    {
        _defaultResult = result;
        return this;
    }

    public FakePermissionService Allow(Guid userId, Guid workspaceId)
    {
        _authorizationResults[(userId, workspaceId)] = true;
        return this;
    }

    public FakePermissionService Deny(Guid userId, Guid workspaceId)
    {
        _authorizationResults[(userId, workspaceId)] = false;
        return this;
    }

    public Task<PermissionDecision> EvaluateAsync(PermissionContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PermissionDecision(
            IsAllowed: _defaultResult,
            ReasonCode: _defaultResult ? null : "ACCESS_DENIED"));
    }

    public async Task EnsureAllowedAsync(PermissionContext context, CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(context, cancellationToken);
        if (!decision.IsAllowed)
            throw new UnauthorizedAccessException(decision.ReasonCode ?? "Access denied.");
    }

    public Task<bool> AuthorizeAsync(Guid userId, Guid workspaceId, ResourceType resourceType, Guid resourceId, PermissionAction action, CancellationToken cancellationToken = default)
    {
        var key = (userId, workspaceId);
        return Task.FromResult(_authorizationResults.TryGetValue(key, out var result) ? result : _defaultResult);
    }

    public Task<bool> AuthorizeWorkspaceAsync(Guid userId, Guid workspaceId, PermissionAction action, CancellationToken cancellationToken = default)
    {
        var key = (userId, workspaceId);
        return Task.FromResult(_authorizationResults.TryGetValue(key, out var result) ? result : _defaultResult);
    }

    public Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, ResourceType resourceType, Guid? resourceId, PermissionAction action, CancellationToken cancellationToken = default)
    {
        var key = (userId, workspaceId);
        return Task.FromResult(_authorizationResults.TryGetValue(key, out var result) ? result : _defaultResult);
    }
}
