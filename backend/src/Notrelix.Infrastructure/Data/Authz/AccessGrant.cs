using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Authz;

public sealed class AccessGrant
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string SourceContext { get; private set; } = "Workspace";
    public string MembershipStatus { get; private set; } = null!;
    public string[] RoleCodes { get; private set; } = [];
    public string[] PermissionCodes { get; private set; } = [];
    public bool IsAccountAdmin { get; private set; }
    public bool IsWorkspaceAdmin { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? SourceEventId { get; private set; }
    public long SourceVersion { get; private set; } = 1;
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");

    private AccessGrant() { }

    public AccessGrant(
        Guid accountId,
        Guid? workspaceId,
        Guid userId,
        string sourceContext,
        string membershipStatus,
        string[] roleCodes,
        string[] permissionCodes,
        bool isAccountAdmin,
        bool isWorkspaceAdmin,
        DateTimeOffset grantedAt,
        Guid? sourceEventId = null,
        long sourceVersion = 1,
        JsonDocument? metadataJson = null)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        WorkspaceId = workspaceId;
        UserId = userId;
        SourceContext = sourceContext;
        MembershipStatus = membershipStatus;
        RoleCodes = roleCodes;
        PermissionCodes = permissionCodes;
        IsAccountAdmin = isAccountAdmin;
        IsWorkspaceAdmin = isWorkspaceAdmin;
        GrantedAt = grantedAt;
        UpdatedAt = grantedAt;
        SourceEventId = sourceEventId;
        SourceVersion = sourceVersion;
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        RevokedAt = revokedAt;
        UpdatedAt = revokedAt;
    }

    public void Update(
        string membershipStatus,
        string[] roleCodes,
        string[] permissionCodes,
        bool isAccountAdmin,
        bool isWorkspaceAdmin,
        DateTimeOffset updatedAt,
        Guid? sourceEventId = null,
        long sourceVersion = 1)
    {
        MembershipStatus = membershipStatus;
        RoleCodes = roleCodes;
        PermissionCodes = permissionCodes;
        IsAccountAdmin = isAccountAdmin;
        IsWorkspaceAdmin = isWorkspaceAdmin;
        UpdatedAt = updatedAt;
        SourceEventId = sourceEventId;
        SourceVersion = sourceVersion;
    }
}
