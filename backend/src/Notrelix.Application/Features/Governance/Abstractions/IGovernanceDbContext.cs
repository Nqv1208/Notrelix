using Notrelix.Domain.Governance.Templates;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.Governance.Abstractions;

public interface IGovernanceDbContext
{
    DbSet<ResourcePermission> ResourcePermissions { get; }
    DbSet<FieldPermission> FieldPermissions { get; }
    DbSet<PermissionRule> PermissionRules { get; }
    DbSet<CustomRole> CustomRoles { get; }
    DbSet<CustomRolePermission> CustomRolePermissions { get; }
    DbSet<MemberRoleAssignment> MemberRoleAssignments { get; }
    DbSet<ShareLink> ShareLinks { get; }
    DbSet<WorkspacePolicy> WorkspacePolicies { get; }
    DbSet<PermissionTemplate> PermissionTemplates { get; }
}