namespace Notrelix.Application.Common.Context;

public sealed class CurrentRequestContext : ICurrentRequestContext
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenantContext _tenant;

    public CurrentRequestContext(ICurrentUser currentUser, ICurrentTenantContext tenant)
    {
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public Guid UserId => _currentUser.UserId;
    public string Email => _currentUser.Email;
    public string Name => _currentUser.Name;
    public bool IsAuthenticated => _currentUser.IsAuthenticated;
    public bool IsSystemContext => _tenant.IsSystemContext;

    public Guid RequireAccountId() => _tenant.RequireAccountId();
    public Guid RequireWorkspaceId() => _tenant.RequireWorkspaceId();
}
