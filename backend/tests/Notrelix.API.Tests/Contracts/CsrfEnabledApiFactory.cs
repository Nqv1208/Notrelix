using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// API host factory with Security:Csrf:Enabled=true for CSRF enforcement tests.
/// </summary>
public sealed class CsrfEnabledApiFactory : NotrelixApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Csrf:Enabled"] = "true"
            }));
    }
}
