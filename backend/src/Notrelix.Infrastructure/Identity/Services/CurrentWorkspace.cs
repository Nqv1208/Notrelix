using Microsoft.AspNetCore.Http;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Identity.Services;

public class CurrentWorkspace : ICurrentWorkspace
{
    private const string ItemsKey = "CurrentWorkspaceId";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _explicitWorkspaceId;

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

    public void SetWorkspace(Guid workspaceId)
    {
        _explicitWorkspaceId = workspaceId;
    }
}
