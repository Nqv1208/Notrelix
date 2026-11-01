using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Notrelix.API;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Core;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>The production host wired to the application-role database.</summary>
internal static class AppRoleHost
{
    public static WebApplicationFactory<Program> CreateFactory(
        AppRoleTestContainer fixture, string db, IReadOnlyDictionary<string, string?>? extra = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(l =>
            {
                l.ClearProviders();
                // NOTRELIX_APP_ROLE_LOG=1 prints server errors, to diagnose a flow that fails under the role.
                if (Environment.GetEnvironmentVariable("NOTRELIX_APP_ROLE_LOG") == "1")
                {
                    l.AddSimpleConsole();
                    l.SetMinimumLevel(LogLevel.Error);
                }
                else
                {
                    l.SetMinimumLevel(LogLevel.Critical);
                }
            });
            builder.UseEnvironment("Testing");
            foreach (var (k, v) in RateLimitTestSettings.Generous) builder.UseSetting(k, v);
            var keys = Path.Combine(Path.GetTempPath(), "notrelix-rls-conf-keys", Guid.NewGuid().ToString("N"));
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = db,
                // Background work (dispatchers, deduplication, consumers, sweeps) connects as the worker login
                // whenever the host serves as the application role; a control host on the owner uses it for both.
                ["ConnectionStrings:NotrelixWorkerDb"] = db == fixture.AppConnectionString ? fixture.WorkerConnectionString : db,
                // The platform operator console: its own login and its own signing key (never the tenant key).
                ["ConnectionStrings:NotrelixOperatorDb"] = fixture.OperatorConnectionString,
                ["Operator:Jwt:SigningKey"] = "operator-console-signing-key-NotTheTenantKey-0123456789",
                ["ConnectionStrings:Redis"] = fixture.RedisConnectionString,
                ["JwtSettings:SecretKey"] = "rls-conformance-signing-key-ThisIsNotASecret-0123456789",
                ["JwtSettings:Issuer"] = "https://notrelix.test",
                ["JwtSettings:Audience"] = "notrelix-api",
                ["Cors:AllowedOrigins:0"] = "https://app.notrelix.test",
                ["Frontend:AppBaseUrl"] = "https://app.notrelix.test",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
                // The role guard is on (the default) for every host that runs as the real roles; only the
                // owner control host, which exists to prove a flow works without row-level security, turns it off.
                ["Rls:RoleEnforcement"] = db == fixture.AppConnectionString ? "Enforce" : "Off",
                ["Smtp:Enabled"] = "true",
                ["Smtp:Host"] = "localhost",
                ["Smtp:Port"] = "587",
                ["Smtp:FromEmail"] = "graph@notrelix.test",
                ["DataProtection:PersistKeys"] = "true",
                ["DataProtection:KeysPath"] = keys,
                ["Messaging:Transport"] = "InMemory",
                ["Billing:Mode"] = "Database",
                ["Storage:Mode"] = "Local",
                ["Security:Csrf:Enabled"] = "false",
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
            };
            if (extra is not null) foreach (var (k, v) in extra) settings[k] = v;
            foreach (var (k, v) in settings) builder.UseSetting(k, v);
        });
}
