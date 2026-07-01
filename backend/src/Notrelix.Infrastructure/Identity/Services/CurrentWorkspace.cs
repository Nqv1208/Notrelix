using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Identity.Services;

public class CurrentWorkspace : ICurrentWorkspace
{
    private const string ItemsKey = "CurrentWorkspaceId";
    private const string SystemContextKey = "SystemContext";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _explicitWorkspaceId;
    private bool _explicitSystemContext;

    public CurrentWorkspace(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? WorkspaceId
    {
        get
        {
            if (_explicitWorkspaceId.HasValue)
                return _explicitWorkspaceId.Value;

            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue(ItemsKey, out var value) == true && value is Guid guid)
                return guid;

            return null;
        }
    }

    public bool IsSet => _explicitWorkspaceId.HasValue
        || _httpContextAccessor.HttpContext?.Items.ContainsKey(ItemsKey) == true;

    public bool IsSystemContext => _explicitSystemContext
        || _httpContextAccessor.HttpContext?.Items.ContainsKey(SystemContextKey) == true;

    public void SetWorkspace(Guid workspaceId)
    {
        _explicitWorkspaceId = workspaceId;
    }

    public IDisposable EnterSystemContext()
    {
        _explicitSystemContext = true;
        return new SystemContextScope(this);
    }

    private sealed class SystemContextScope : IDisposable
    {
        private readonly CurrentWorkspace _owner;
        public SystemContextScope(CurrentWorkspace owner) => _owner = owner;
        public void Dispose() => _owner._explicitSystemContext = false;
    }
}
