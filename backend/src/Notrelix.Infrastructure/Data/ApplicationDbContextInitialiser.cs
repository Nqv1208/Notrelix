using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Billing;
using Notrelix.Infrastructure.Data.Seed;
using Notrelix.Infrastructure.Operators;
using Notrelix.Domain.Operators;


namespace Notrelix.Infrastructure.Data;

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly SeedDataOptions _options;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RlsPolicyApplier _rlsPolicyApplier;
    private readonly ICurrentTenantContext _tenant;
    private readonly RlsOptions _rlsOptions;
    private readonly OperatorOptions _operatorOptions;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<SeedDataOptions> options,
        RlsPolicyApplier rlsPolicyApplier,
        ICurrentTenantContext tenant,
        IOptions<RlsOptions> rlsOptions,
        IOptions<OperatorOptions>? operatorOptions = null)
    {
        _operatorOptions = operatorOptions?.Value ?? new OperatorOptions();
        _logger = logger;
        _context = context;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _rlsPolicyApplier = rlsPolicyApplier;
        _tenant = tenant;
        _rlsOptions = rlsOptions.Value;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsNpgsql())
            {
                await _context.Database.MigrateAsync();

                if (_rlsOptions.Enabled && _rlsOptions.ApplyPoliciesOnStartup)
                {
                    await _rlsPolicyApplier.ApplyAsync();
                }

                // Baseline data every environment needs, written on the owner connection and only when absent:
                // the plan catalog (an account without a subscription is on its Free plan) and the one seeded
                // operator account of the Notrelix company.
                await PlanCatalogSeed.EnsureAsync(_context);
                await SeedOperatorAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    /// <summary>
    /// Creates the seeded operator account when <c>Operator:Seed</c> carries an email and a password and no operator
    /// holds that email yet. The password is read from configuration, hashed, and neither logged nor returned; an
    /// existing operator is never overwritten, so rotating the configured password does not silently reset an account.
    /// </summary>
    private async Task SeedOperatorAsync()
    {
        var seed = _operatorOptions.Seed;
        if (!seed.IsConfigured)
        {
            return;
        }

        var email = seed.Email.Trim().ToLowerInvariant();
        if (await _context.OperatorAccounts.AnyAsync(o => o.Email == email))
        {
            return;
        }

        _context.OperatorAccounts.Add(OperatorAccount.Create(
            seed.Email, seed.DisplayName, _passwordHasher.HashPassword(seed.Password), DateTimeOffset.UtcNow));
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded the operator account '{Email}'.", email);
    }

    public async Task SeedAsync()
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SeedData is disabled. Skipping seed pipeline.");
            return;
        }

        _tenant.SetSystem();
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
        finally
        {
            _tenant.Clear();
        }
    }

    /// <summary>
    /// Development only (the caller guards the environment): heals a development database that predates
    /// the current seed. Touches the seed accounts only, deletes nothing and is a no-op when nothing needs
    /// repair.
    /// </summary>
    public async Task RepairDevelopmentSeedAsync()
    {
        if (!_options.Enabled)
        {
            return;
        }

        _tenant.SetSystem();
        try
        {
            var result = await DevelopmentSeedRepair.RunAsync(_context, DateTimeOffset.UtcNow);
            if (result.ChangedAnything)
            {
                _logger.LogInformation("Development seed repair applied changes.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while repairing the development seed data.");
            throw;
        }
        finally
        {
            _tenant.Clear();
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
        _context.SpacePaths.RemoveRange(await _context.SpacePaths.IgnoreQueryFilters().ToListAsync());
        _context.SpaceMembers.RemoveRange(await _context.SpaceMembers.IgnoreQueryFilters().ToListAsync());
        _context.Spaces.RemoveRange(await _context.Spaces.IgnoreQueryFilters().ToListAsync());
        _context.WorkspaceMembers.RemoveRange(await _context.WorkspaceMembers.IgnoreQueryFilters().ToListAsync());
        _context.WorkspaceInvitations.RemoveRange(await _context.WorkspaceInvitations.IgnoreQueryFilters().ToListAsync());
        _context.Workspaces.RemoveRange(await _context.Workspaces.IgnoreQueryFilters().ToListAsync());
        _context.Sessions.RemoveRange(await _context.Sessions.IgnoreQueryFilters().ToListAsync());
        _context.UserProfiles.RemoveRange(await _context.UserProfiles.IgnoreQueryFilters().ToListAsync());
        _context.Users.RemoveRange(await _context.Users.IgnoreQueryFilters().ToListAsync());
        await _context.SaveChangesAsync();
    }

}
