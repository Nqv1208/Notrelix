using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Governance public target action: the creator's explicit Manager entry on a
/// freshly created resource. Added to the request's unit of work, so the
/// resource and its creator entry commit together.
/// </summary>
public sealed class ResourceCreatorGrantAdapter : IResourceCreatorGrant
{
    private readonly ApplicationDbContext _db;

    public ResourceCreatorGrantAdapter(ApplicationDbContext db) => _db = db;

    public void GrantManager(
        Guid accountId,
        Guid workspaceId,
        string resourceKind,
        Guid resourceId,
        Guid creatorUserId,
        DateTimeOffset now)
    {
        _db.ResourcePermissions.Add(ResourcePermission.Grant(
            accountId, workspaceId, ResourceKind.Create(resourceKind), resourceId,
            PermissionSubjectType.User, creatorUserId, PermissionLevel.Manager,
            creatorUserId, now));
    }
}
