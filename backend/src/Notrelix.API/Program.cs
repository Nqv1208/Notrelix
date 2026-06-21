using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Middleware;
using Notrelix.API.Endpoints;
using Notrelix.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

if (args.Length > 0 && args[0] is "--seed" or "--migrate")
{
    var connectionString = builder.Configuration.GetConnectionString("NotrelixDb");
    builder.Services
        .Configure<SeedDataOptions>(builder.Configuration.GetSection("SeedData"))
        .AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            }));

    if (args[0] == "--seed")
    {
        builder.Services.AddSingleton<Notrelix.Application.Common.Abstractions.IPasswordHasher,
            Notrelix.Infrastructure.Auth.Passwords.PasswordHasher>();
    }

    var seedApp = builder.Build();

    using var scope = seedApp.Services.CreateScope();
    var sp = scope.ServiceProvider;

    if (args[0] == "--migrate")
    {
        var ctx = sp.GetRequiredService<ApplicationDbContext>();
        await ctx.Database.MigrateAsync();
        Console.WriteLine("Migration completed successfully.");
        return;
    }

    if (args[0] == "--seed")
    {
        var opts = sp.GetRequiredService<IOptions<SeedDataOptions>>();
        if (!opts.Value.Enabled)
        {
            Console.WriteLine("SeedData is disabled in configuration. Skipping.");
            return;
        }

        var ctx = sp.GetRequiredService<ApplicationDbContext>();
        var hasher = sp.GetRequiredService<Notrelix.Application.Common.Abstractions.IPasswordHasher>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ApplicationDbContextInitialiser>();
        var initialiser = new ApplicationDbContextInitialiser(logger, ctx, hasher, opts);
        await initialiser.SeedAsync();
        Console.WriteLine("Seed completed successfully.");
        return;
    }
}

builder.AddApplicationServices();
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApiLayer(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseWorkspaceResolution();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
