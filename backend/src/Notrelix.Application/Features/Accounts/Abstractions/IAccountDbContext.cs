using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Invitations;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Accounts.Regions;
using Notrelix.Domain.Accounts.Scim;
using Notrelix.Domain.Accounts.Settings;
using Notrelix.Domain.Accounts.WorkspaceRoutes;

namespace Notrelix.Application.Features.Accounts.Abstractions;

public interface IAccountDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<AccountMember> AccountMembers { get; }
    DbSet<AccountInvitation> AccountInvitations { get; }
    DbSet<AccountDomain> AccountDomains { get; }
    DbSet<AccountSettings> AccountSettingsEntities { get; }
    DbSet<AccountRegion> AccountRegions { get; }
    DbSet<AccountIdentityProvider> AccountIdentityProviders { get; }
    DbSet<ScimDirectory> ScimDirectories { get; }
    DbSet<ScimSyncRun> ScimSyncRuns { get; }
    DbSet<WorkspaceRoute> WorkspaceRoutes { get; }
}
