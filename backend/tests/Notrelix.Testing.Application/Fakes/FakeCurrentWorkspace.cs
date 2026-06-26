using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Testing.Application.Fakes;

public sealed class FakeCurrentWorkspace : ICurrentWorkspace
{
    public Guid? WorkspaceId { get; private set; }
    public bool IsSet => WorkspaceId.HasValue;
    public bool IsSystemContext { get; private set; }
    public bool HasWorkspace => WorkspaceId.HasValue;

    public void SetWorkspace(Guid workspaceId)
    {
        WorkspaceId = workspaceId;
        IsSystemContext = false;
    }

    public void Clear()
    {
        WorkspaceId = null;
        IsSystemContext = false;
    }

    public IDisposable EnterSystemContext()
    {
        var previous = IsSystemContext;
        IsSystemContext = true;
        return new SystemContextReset(() => IsSystemContext = previous);
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
