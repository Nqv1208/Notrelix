using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Notrelix.API.Endpoints;
using Notrelix.API.Extensions;
using Notrelix.API.Middleware;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Middleware;
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
    .AddInfrastructure(builder.Configuration)
    .AddApiLayer(builder.Configuration, builder.Environment);

var app = builder.Build();

if (await app.RunDatabaseCommandsAsync(args))
{
    return;
}

await app.InitialiseDatabaseOnStartupAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

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
app.UseMiddleware<NotrelixRateLimitingMiddleware>();
app.UseWorkspaceResolution();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
