using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Abstractions.Rls;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Data.Seed;
using Notrelix.Infrastructure.Data.Platform;
using Notrelix.Infrastructure.Data.Product;
using Notrelix.Infrastructure.Data.Projection;
using Notrelix.Infrastructure.Data.Runtime;

namespace Notrelix.Infrastructure.Data;

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly PlatformDbContext? _platform;
    private readonly ProductDbContext? _product;
    private readonly ProjectionDbContext? _projection;
    private readonly InfrastructureDbContext? _infrastructure;
    private readonly SeedDataOptions _options;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RlsPolicyApplier _rlsPolicyApplier;
    private readonly ICurrentWorkspace _currentWorkspace;
    private readonly RlsOptions _rlsOptions;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<SeedDataOptions> options,
        RlsPolicyApplier rlsPolicyApplier,
        ICurrentWorkspace currentWorkspace,
        IOptions<RlsOptions> rlsOptions,
        PlatformDbContext? platform = null,
        ProductDbContext? product = null,
        ProjectionDbContext? projection = null,
        InfrastructureDbContext? infrastructure = null)
    {
        _logger = logger;
        _context = context;
        _platform = platform;
        _product = product;
        _projection = projection;
        _infrastructure = infrastructure;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _rlsPolicyApplier = rlsPolicyApplier;
        _currentWorkspace = currentWorkspace;
        _rlsOptions = rlsOptions.Value;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsNpgsql())
            {
                // Migrate split contexts if available, then legacy context
                if (_platform is not null) await _platform.Database.MigrateAsync();
                if (_product is not null) await _product.Database.MigrateAsync();
                if (_projection is not null) await _projection.Database.MigrateAsync();
                if (_infrastructure is not null) await _infrastructure.Database.MigrateAsync();
                await _context.Database.MigrateAsync();

                if (_rlsOptions.Enabled)
                {
                    await ApplyRlsFoundationAsync();
                }

                if (_rlsOptions.Enabled && _rlsOptions.ApplyPoliciesOnStartup)
                {
                    await _rlsPolicyApplier.ApplyAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SeedData is disabled. Skipping seed pipeline.");
            return;
        }

        using var systemScope = _currentWorkspace.EnterSystemContext();
        try
        {
            var result = await TrySeedAsync();
            _logger.LogInformation(
                "Seed completed. Users={Users}, Workspaces={Workspaces}, Boards={Boards}, " +
                "Items={Items}, Pages={Pages}, Comments={Comments}, Notifications={Notifications}, Skipped={Skipped}",
                result.UsersCreated, result.WorkspacesCreated, result.BoardsCreated,
                result.BoardItemsCreated, result.PagesCreated, result.CommentsCreated,
                result.NotificationsCreated, result.Skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task<SeedResult> TrySeedAsync()
    {
        var targets = _options.GetTargets();

        if (_options.ResetBeforeSeed)
        {
            _logger.LogInformation("Resetting seed data before re-seeding...");
            await ResetSeedDataAsync();
        }

        return await InitDb.SeedAsync(_context, targets, _passwordHasher);
    }

    private async Task ResetSeedDataAsync()
    {
        _context.NotificationRecipients.RemoveRange(await _context.NotificationRecipients.IgnoreQueryFilters().ToListAsync());
        _context.NotificationItems.RemoveRange(await _context.NotificationItems.IgnoreQueryFilters().ToListAsync());
        _context.Comments.RemoveRange(await _context.Comments.IgnoreQueryFilters().ToListAsync());
        _context.Blocks.RemoveRange(await _context.Blocks.IgnoreQueryFilters().ToListAsync());
        _context.Pages.RemoveRange(await _context.Pages.IgnoreQueryFilters().ToListAsync());
        _context.BoardItemMembers.RemoveRange(await _context.BoardItemMembers.IgnoreQueryFilters().ToListAsync());
        _context.BoardItemLabels.RemoveRange(await _context.BoardItemLabels.IgnoreQueryFilters().ToListAsync());
        _context.BoardItemValues.RemoveRange(await _context.BoardItemValues.IgnoreQueryFilters().ToListAsync());
        _context.BoardItems.RemoveRange(await _context.BoardItems.IgnoreQueryFilters().ToListAsync());
        _context.BoardViewUserPreferences.RemoveRange(await _context.BoardViewUserPreferences.IgnoreQueryFilters().ToListAsync());
        _context.SavedFilters.RemoveRange(await _context.SavedFilters.IgnoreQueryFilters().ToListAsync());
        _context.BoardViewPins.RemoveRange(await _context.BoardViewPins.IgnoreQueryFilters().ToListAsync());
        _context.BoardViews.RemoveRange(await _context.BoardViews.IgnoreQueryFilters().ToListAsync());
        _context.FieldOptions.RemoveRange(await _context.FieldOptions.IgnoreQueryFilters().ToListAsync());
        _context.BoardFields.RemoveRange(await _context.BoardFields.IgnoreQueryFilters().ToListAsync());
        _context.BoardGroups.RemoveRange(await _context.BoardGroups.IgnoreQueryFilters().ToListAsync());
        _context.Labels.RemoveRange(await _context.Labels.IgnoreQueryFilters().ToListAsync());
        _context.Boards.RemoveRange(await _context.Boards.IgnoreQueryFilters().ToListAsync());
        _context.WorkspaceMembers.RemoveRange(await _context.WorkspaceMembers.IgnoreQueryFilters().ToListAsync());
        _context.WorkspaceInvitations.RemoveRange(await _context.WorkspaceInvitations.IgnoreQueryFilters().ToListAsync());
        _context.Workspaces.RemoveRange(await _context.Workspaces.IgnoreQueryFilters().ToListAsync());
        _context.Sessions.RemoveRange(await _context.Sessions.IgnoreQueryFilters().ToListAsync());
        _context.UserProfiles.RemoveRange(await _context.UserProfiles.IgnoreQueryFilters().ToListAsync());
        _context.Users.RemoveRange(await _context.Users.IgnoreQueryFilters().ToListAsync());
        await _context.SaveChangesAsync();
    }

    private async Task ApplyRlsFoundationAsync()
    {
        var assembly = typeof(ApplicationDbContextInitialiser).Assembly;
        var connection = (Npgsql.NpgsqlConnection)_context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        var scriptNames = new[]
        {
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.001_roles.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.002_helpers.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.003_authz_projection.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.004_grants.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.005_policies_identity.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.006_policies_workspace_governance_authz.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.007_policies_workspace_scoped_domain.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.008_policies_events.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.009_policies_messaging.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.010_policies_notifications.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.011_policies_activity.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.012_policies_audit.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.013_policies_projection.sql",
            "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.014_policies_ops.sql",
        };

        foreach (var resourceName in scriptNames)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                _logger.LogWarning("RLS SQL resource '{Resource}' not found. Skipping.", resourceName);
                continue;
            }
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync();
            await using var cmd = new Npgsql.NpgsqlCommand(sql, connection);
            cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("RLS script applied: {Script}", resourceName);
        }
    }
}
