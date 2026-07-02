using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;

// Account
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Invitations;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Accounts.Regions;
using Notrelix.Domain.Accounts.Scim;
using Notrelix.Domain.Accounts.Settings;
using Notrelix.Domain.Accounts.WorkspaceRoutes;

// Identity
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;

// Workspace
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Workspaces;

// Governance
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Policies;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Governance.Templates;

// Authz (Infrastructure entities)
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Governance.Projections;

namespace Notrelix.Infrastructure.Data.Platform;

/// <summary>
/// DbContext for platform bounded-contexts: Account, Identity, Workspace, Governance, Authz.
/// Schemas: account, identity, workspace, governance, authz
/// </summary>
public class PlatformDbContext : BaseNotrelixDbContext,
    IAccountDbContext, IIdentityDbContext, IWorkspaceDbContext, IGovernanceDbContext
{
    public PlatformDbContext(
        DbContextOptions<PlatformDbContext> options,
        ICurrentWorkspace? currentWorkspace = null)
        : base(options, currentWorkspace) { }

    // Account
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountMember> AccountMembers => Set<AccountMember>();
    public DbSet<AccountInvitation> AccountInvitations => Set<AccountInvitation>();
    public DbSet<AccountDomain> AccountDomains => Set<AccountDomain>();
    public DbSet<AccountSettings> AccountSettingsEntities => Set<AccountSettings>();
    public DbSet<AccountRegion> AccountRegions => Set<AccountRegion>();
    public DbSet<AccountIdentityProvider> AccountIdentityProviders => Set<AccountIdentityProvider>();
    public DbSet<ScimDirectory> ScimDirectories => Set<ScimDirectory>();
    public DbSet<ScimSyncRun> ScimSyncRuns => Set<ScimSyncRun>();
    public DbSet<WorkspaceRoute> WorkspaceRoutes => Set<WorkspaceRoute>();

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();
    public DbSet<UserSecuritySettings> UserSecuritySettings => Set<UserSecuritySettings>();
    public DbSet<UserMfaMethod> UserMfaMethods => Set<UserMfaMethod>();
    public DbSet<UserLoginAttempt> UserLoginAttempts => Set<UserLoginAttempt>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();

    // Workspace
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    // Governance
    public DbSet<ResourcePermission> ResourcePermissions => Set<ResourcePermission>();
    public DbSet<FieldPermission> FieldPermissions => Set<FieldPermission>();
    public DbSet<PermissionRule> PermissionRules => Set<PermissionRule>();
    public DbSet<CustomRole> CustomRoles => Set<CustomRole>();
    public DbSet<CustomRolePermission> CustomRolePermissions => Set<CustomRolePermission>();
    public DbSet<MemberRoleAssignment> MemberRoleAssignments => Set<MemberRoleAssignment>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<WorkspacePolicy> WorkspacePolicies => Set<WorkspacePolicy>();
    public DbSet<PermissionTemplate> PermissionTemplates => Set<PermissionTemplate>();
    public DbSet<ResourcePermissionInheritanceCacheEntry> ResourcePermissionInheritanceCache => Set<ResourcePermissionInheritanceCacheEntry>();

    // Authz
    public DbSet<AccessGrant> AccessGrants => Set<AccessGrant>();

    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BaseNotrelixDbContext).Assembly,
            t => t.Namespace is not null && (
                t.Namespace.Contains(".Configurations.Account") ||
                t.Namespace.Contains(".Configurations.Identity") ||
                t.Namespace.Contains(".Configurations.Workspace") ||
                t.Namespace.Contains(".Configurations.Governance") ||
                t.Namespace.Contains(".Configurations.Authz")));
    }
}
