using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Identity.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentWorkspace _currentWorkspace;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, ICurrentWorkspace currentWorkspace)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentWorkspace = currentWorkspace;
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var id =
                user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                user?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(id, out var userId) ? userId : Guid.Empty;
        }
    }

    public string Email
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return
                user?.FindFirstValue(JwtRegisteredClaimNames.Email) ??
                user?.FindFirstValue(ClaimTypes.Email) ??
                string.Empty;
        }
    }

    public string Name
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return
                user?.FindFirstValue(JwtRegisteredClaimNames.Name) ??
                user?.FindFirstValue(ClaimTypes.Name) ??
                string.Empty;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? WorkspaceId => _currentWorkspace.WorkspaceId;
}
