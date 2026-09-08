using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions.Records;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Invitations;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Accounts.Regions;
using Notrelix.Domain.Accounts.Scim;
using Notrelix.Domain.Accounts.WorkspaceRoutes;

namespace Notrelix.Application.Tests.Features.Accounts.Members;

/// <summary>
/// Test-only Account context on EF InMemory. The membership action's
/// idempotency-within-transaction semantics live in the EF change tracker,
/// which a mocked DbSet cannot exercise — this stub keeps the proof honest.
/// </summary>
public sealed class TestAccountDbContext(DbContextOptions<TestAccountDbContext> options)
    : DbContext(options), IAccountDbContext
{
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<AccountMember> AccountMembers { get; set; } = null!;
    public DbSet<AccountInvitation> AccountInvitations { get; set; } = null!;
    public DbSet<AccountDomain> AccountDomains { get; set; } = null!;
    public DbSet<AccountSettingRecord> AccountSettingsEntities { get; set; } = null!;
    public DbSet<AccountRegion> AccountRegions { get; set; } = null!;
    public DbSet<AccountIdentityProvider> AccountIdentityProviders { get; set; } = null!;
    public DbSet<ScimDirectory> ScimDirectories { get; set; } = null!;
    public DbSet<ScimSyncRun> ScimSyncRuns { get; set; } = null!;
    public DbSet<WorkspaceRoute> WorkspaceRoutes { get; set; } = null!;
}
