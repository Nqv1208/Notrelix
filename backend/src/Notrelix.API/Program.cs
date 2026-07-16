using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Notrelix.API.Endpoints;
using Notrelix.API.Extensions;
using Notrelix.API.Middleware;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Options;
using Dpo = Notrelix.Infrastructure.Options.DataProtectionOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
    options.ShutdownTimeout = TimeSpan.FromSeconds(30));

builder.Services
    .AddOptions<Dpo>()
    .Bind(builder.Configuration.GetSection("DataProtection"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<Dpo>>(
    _ => new DataProtectionOptionsValidator(builder.Environment.EnvironmentName));

builder.AddApplicationServices();

var dataProtectionOptions = builder.Configuration
    .GetSection("DataProtection")
    .Get<Dpo>() ?? new Dpo();

var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName(dataProtectionOptions.ApplicationName);

if (dataProtectionOptions.PersistKeys && !string.IsNullOrWhiteSpace(dataProtectionOptions.KeysPath))
{
    Directory.CreateDirectory(dataProtectionOptions.KeysPath);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionOptions.KeysPath));
}

builder.Services
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddApiLayer(builder.Configuration, builder.Environment);

var app = builder.Build();

if (await app.RunDatabaseCommandsAsync(args))
{
    return;
}

await app.InitialiseDatabaseOnStartupAsync();

// 1. Forwarded headers (proxy support)
app.UseForwardedHeaders();

// 2. Exception handler (global error handling)
app.UseExceptionHandler();

// 3. Correlation ID (request tracing)
app.UseMiddleware<CorrelationIdMiddleware>();

// 4. CSRF validation (before rate limiting, after correlation)
app.UseMiddleware<CsrfValidationMiddleware>();

// 5. Security headers (transport security)
app.UseMiddleware<SecurityHeadersMiddleware>();

// 6. Rate limiting (before auth, after security headers)
app.UseMiddleware<PreAuthenticationRateLimitMiddleware>();

// 7. HSTS (non-dev only)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// 8. Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 8. CORS
app.UseCors("Frontend");

// 9. HTTPS redirection (conditional)
if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

// 10. Authentication
app.UseAuthentication();

// 11. HTTP request context (populate IExecutionContext from JWT claims)
app.UseMiddleware<HttpRequestContextMiddleware>();

// 12. Rate limiting (authenticated)
app.UseMiddleware<AuthenticatedRateLimitMiddleware>();

// 13. Security audit (capture auth failures, rate limits)
app.UseMiddleware<SecurityAuditMiddleware>();

// 14. Authorization
app.UseAuthorization();

// 15. Endpoints
app.MapEndpoints();

app.Run();
