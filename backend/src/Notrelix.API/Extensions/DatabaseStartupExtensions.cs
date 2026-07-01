using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;

namespace Notrelix.API.Extensions;

public static class DatabaseStartupExtensions
{
    public static async Task<bool> RunDatabaseCommandsAsync(
        this WebApplication app,
        string[] args)
    {
        var shouldMigrate = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase)
                            || args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase);
        var shouldSeed = args.Contains("--seed", StringComparer.OrdinalIgnoreCase);
        var shouldApplyRls = args.Contains("--rls-apply", StringComparer.OrdinalIgnoreCase);

        if (!shouldMigrate && !shouldSeed && !shouldApplyRls)
        {
            return false;
        }

        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContextInitialiser>();

        if (shouldMigrate)
        {
            await initialiser.InitialiseAsync();
            app.Logger.LogInformation("Database migration completed.");
        }

        if (shouldApplyRls)
        {
            var applier = scope.ServiceProvider.GetRequiredService<RlsPolicyApplier>();
            await applier.ApplyAsync();
            app.Logger.LogInformation("RLS policies applied.");
        }

        if (shouldSeed)
        {
            var seedOptions = scope.ServiceProvider
                .GetRequiredService<IOptions<SeedDataOptions>>()
                .Value;

            if (!seedOptions.Enabled)
            {
                app.Logger.LogInformation("SeedData is disabled. Skipping seed.");
            }
            else
            {
                await initialiser.SeedAsync();
                app.Logger.LogInformation("Database seed completed.");
            }
        }

        return true;
    }

    public static async Task InitialiseDatabaseOnStartupAsync(
        this WebApplication app)
    {
        var migrateOnStartup = app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
        var seedEnabled = app.Configuration.GetValue<bool>("SeedData:Enabled");
        var seedRunOnStartup = app.Configuration.GetValue<bool>("SeedData:RunOnStartup");

        if (!migrateOnStartup && !(seedEnabled && seedRunOnStartup))
        {
            return;
        }

        if (!app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning(
                "Database startup initialization is enabled outside Development. Ensure this is intentional.");
        }

        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContextInitialiser>();

        if (migrateOnStartup)
        {
            await initialiser.InitialiseAsync();
            app.Logger.LogInformation("Startup database migration completed.");
        }

        if (seedEnabled && seedRunOnStartup)
        {
            await initialiser.SeedAsync();
            app.Logger.LogInformation("Startup database seed completed.");
        }
    }
}
