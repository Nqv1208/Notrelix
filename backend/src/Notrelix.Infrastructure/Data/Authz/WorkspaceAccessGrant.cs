using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Authz;

public sealed class WorkspaceAccessGrant
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string SourceContext { get; private set; } = "Workspace";
    public string MembershipStatus { get; private set; } = null!;
    public string[] RoleCodes { get; private set; } = [];
    public string[] PermissionCodes { get; private set; } = [];
    public bool IsWorkspaceAdmin { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? SourceEventId { get; private set; }
    public long? SourceVersion { get; private set; }
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");

    private WorkspaceAccessGrant() { }

    public WorkspaceAccessGrant(
        Guid workspaceId,
        Guid userId,
        string sourceContext,
        string membershipStatus,
        string[] roleCodes,
        string[] permissionCodes,
        bool isWorkspaceAdmin,
        DateTimeOffset grantedAt,
        Guid? sourceEventId,
        long? sourceVersion,
        JsonDocument? metadataJson)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        SourceContext = sourceContext;
        MembershipStatus = membershipStatus;
        RoleCodes = roleCodes;
        PermissionCodes = permissionCodes;
        IsWorkspaceAdmin = isWorkspaceAdmin;
        GrantedAt = grantedAt;
        UpdatedAt = grantedAt;
        SourceEventId = sourceEventId;
        SourceVersion = sourceVersion;
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
    }
}
