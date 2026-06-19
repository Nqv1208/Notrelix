using Microsoft.AspNetCore.HttpOverrides;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Middleware;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services — NO AddControllers(), using Minimal API exclusively
builder.Services.AddInfrastructure(builder.Configuration);
builder.AddApplicationServices();
builder.AddWebServices();

// Error handling — ProblemDetails RFC 7807
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    // Containers sit behind nginx / edge proxies with dynamic network addresses.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Initialise database and run opt-in seed data.
using var scope = app.Services.CreateScope();
var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

await initialiser.InitialiseAsync();

if (app.Configuration.GetValue<bool>("SeedData:Enabled"))
{
    await initialiser.SeedAsync();
}

// Middleware pipeline
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

// TLS is terminated by nginx / the edge proxy in container deployments.
// Enable only when the API itself is responsible for HTTPS redirection.
if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseWorkspaceResolution();
app.UseAuthorization();

// ── Minimal API endpoints (replaces app.MapControllers()) ────
app.MapEndpoints();

app.Run();
