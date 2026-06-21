using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data.Seed;

namespace Notrelix.Infrastructure.Data;

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly SeedDataOptions _options;
    private readonly IPasswordHasher _passwordHasher;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<SeedDataOptions> options)
    {
        _logger = logger;
        _context = context;
        _passwordHasher = passwordHasher;
        _options = options.Value;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsNpgsql())
            {
                await _context.Database.MigrateAsync();
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

        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        var targets = _options.GetTargets();

        if (_options.ResetBeforeSeed)
        {
            _logger.LogInformation("Resetting seed data before re-seeding...");
            await ResetSeedDataAsync();
        }

        await InitDb.SeedAsync(_context, targets, _passwordHasher);
    }

    private async Task ResetSeedDataAsync()
    {
        _context.Notifications.RemoveRange(await _context.Notifications.IgnoreQueryFilters().ToListAsync());
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
}
