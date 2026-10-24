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
            builder.ConfigureLogging(l => { l.ClearProviders(); l.SetMinimumLevel(LogLevel.Critical); });
            builder.UseEnvironment("Testing");
            foreach (var (k, v) in RateLimitTestSettings.Generous) builder.UseSetting(k, v);
            var keys = Path.Combine(Path.GetTempPath(), "notrelix-rls-conf-keys", Guid.NewGuid().ToString("N"));
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = db,
                ["ConnectionStrings:Redis"] = fixture.RedisConnectionString,
                ["JwtSettings:SecretKey"] = "rls-conformance-signing-key-ThisIsNotASecret-0123456789",
                ["JwtSettings:Issuer"] = "https://notrelix.test",
                ["JwtSettings:Audience"] = "notrelix-api",
                ["Cors:AllowedOrigins:0"] = "https://app.notrelix.test",
                ["Frontend:AppBaseUrl"] = "https://app.notrelix.test",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
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
