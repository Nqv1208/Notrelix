using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Identity.Services;

public class CurrentWorkspace : ICurrentWorkspace
{
    private const string AccountIdKey = "CurrentAccountId";
    private const string WorkspaceIdKey = "CurrentWorkspaceId";
    private const string SystemContextKey = "SystemContext";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _explicitAccountId;
    private Guid? _explicitWorkspaceId;
    private bool _explicitSystemContext;

    public CurrentWorkspace(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid AccountId
    {
        get
        {
            if (_explicitAccountId.HasValue)
                return _explicitAccountId.Value;

            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue(AccountIdKey, out var value) == true && value is Guid guid)
                return guid;

            return default;
        }
    }

    public Guid WorkspaceId
    {
        get
        {
            if (_explicitWorkspaceId.HasValue)
                return _explicitWorkspaceId.Value;

            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue(WorkspaceIdKey, out var value) == true && value is Guid guid)
                return guid;

            return default;
        }
    }

    public bool IsSet => _explicitWorkspaceId.HasValue || _explicitAccountId.HasValue
        || _httpContextAccessor.HttpContext?.Items.ContainsKey(WorkspaceIdKey) == true
        || _httpContextAccessor.HttpContext?.Items.ContainsKey(AccountIdKey) == true;

    public bool IsSystemContext => _explicitSystemContext
        || _httpContextAccessor.HttpContext?.Items.ContainsKey(SystemContextKey) == true;

    public void SetWorkspace(Guid accountId, Guid workspaceId)
    {
        _explicitAccountId = accountId;
        _explicitWorkspaceId = workspaceId;
    }

    public IDisposable EnterSystemContext()
    {
        var savedAccountId = _explicitAccountId;
        var savedWorkspaceId = _explicitWorkspaceId;
        _explicitSystemContext = true;
        return new SystemContextScope(this, savedAccountId, savedWorkspaceId);
    }

    private sealed class SystemContextScope : IDisposable
    {
        private readonly CurrentWorkspace _owner;
        private readonly Guid? _savedAccountId;
        private readonly Guid? _savedWorkspaceId;

        public SystemContextScope(CurrentWorkspace owner, Guid? savedAccountId, Guid? savedWorkspaceId)
        {
            _owner = owner;
            _savedAccountId = savedAccountId;
            _savedWorkspaceId = savedWorkspaceId;
        }

        public void Dispose()
        {
            _owner._explicitSystemContext = false;
            _owner._explicitAccountId = _savedAccountId;
            _owner._explicitWorkspaceId = _savedWorkspaceId;
        }
    }
}
