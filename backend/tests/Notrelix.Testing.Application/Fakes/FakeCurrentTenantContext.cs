using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Testing.Application.Fakes;

public sealed class FakeCurrentTenantContext : ICurrentTenantContext
{
    private Guid? _accountId;
    private Guid? _workspaceId;
    private Guid? _userId;
    private bool _isSystemContext;

    public Guid? AccountId => _isSystemContext ? null : _accountId;
    public Guid? WorkspaceId => _isSystemContext ? null : _workspaceId;
    public Guid? UserId => _isSystemContext ? null : _userId;
    public bool IsSystemContext => _isSystemContext;
    public bool IsResolved => _isSystemContext || _accountId.HasValue;

    public Guid RequireAccountId()
    {
        if (_isSystemContext)
            throw new InvalidOperationException("Cannot require AccountId in system context.");
        return _accountId
            ?? throw new InvalidOperationException("Account context has not been resolved.");
    }

    public Guid RequireWorkspaceId()
    {
        if (_isSystemContext)
            throw new InvalidOperationException("Cannot require WorkspaceId in system context.");
        return _workspaceId
            ?? throw new InvalidOperationException("Workspace context has not been resolved.");
    }

    public Guid RequireUserId()
    {
        if (_isSystemContext)
            throw new InvalidOperationException("Cannot require UserId in system context.");
        return _userId
            ?? throw new InvalidOperationException("User context has not been resolved.");
    }

    public void SetAccount(Guid accountId, Guid? userId)
    {
        _accountId = accountId;
        _workspaceId = null;
        _userId = userId;
        _isSystemContext = false;
    }

    public void SetWorkspace(Guid accountId, Guid workspaceId, Guid? userId)
    {
        _accountId = accountId;
        _workspaceId = workspaceId;
        _userId = userId;
        _isSystemContext = false;
    }

    public void SetSystem()
    {
        _accountId = null;
        _workspaceId = null;
        _userId = null;
        _isSystemContext = true;
    }

    public void Clear()
    {
        _accountId = null;
        _workspaceId = null;
        _userId = null;
        _isSystemContext = false;
    }
}
