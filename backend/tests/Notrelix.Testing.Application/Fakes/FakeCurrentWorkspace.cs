using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Testing.Application.Fakes;

public sealed class FakeCurrentWorkspace : ICurrentWorkspace
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public bool IsSet => WorkspaceId != default || AccountId != default;
    public bool IsSystemContext { get; private set; }

    public void SetWorkspace(Guid accountId, Guid workspaceId)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
        IsSystemContext = false;
    }

    public void Clear()
    {
        AccountId = default;
        WorkspaceId = default;
        IsSystemContext = false;
    }

    public IDisposable EnterSystemContext()
    {
        var previousAccountId = AccountId;
        var previousWorkspaceId = WorkspaceId;
        var previousSystemContext = IsSystemContext;
        AccountId = default;
        WorkspaceId = default;
        IsSystemContext = true;
        return new SystemContextReset(() =>
        {
            AccountId = previousAccountId;
            WorkspaceId = previousWorkspaceId;
            IsSystemContext = previousSystemContext;
        });
    }

    private sealed class SystemContextReset : IDisposable
    {
        private readonly Action _reset;
        private bool _disposed;
        public SystemContextReset(Action reset) => _reset = reset;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _reset();
            }
        }
    }
}
