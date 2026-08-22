namespace Notrelix.Testing.Application.Fakes;

public sealed class FakeCurrentRequestContext : ICurrentRequestContext
{
    private readonly FakeCurrentTenantContext _tenant = new();

    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; } = true;
    public bool IsSystemContext { get; set; }
    public Guid? SessionId { get; set; }

    public FakeCurrentTenantContext Tenant => _tenant;

    public Guid RequireAccountId() => _tenant.RequireAccountId();

    public Guid RequireWorkspaceId() => _tenant.RequireWorkspaceId();

    public FakeCurrentRequestContext AsUser(Guid userId, string email = "user@test.local", string name = "Test User")
    {
        UserId = userId;
        Email = email;
        Name = name;
        IsAuthenticated = true;
        return this;
    }

    public FakeCurrentRequestContext AsAnonymous()
    {
        UserId = Guid.Empty;
        Email = string.Empty;
        Name = string.Empty;
        IsAuthenticated = false;
        return this;
    }
}
