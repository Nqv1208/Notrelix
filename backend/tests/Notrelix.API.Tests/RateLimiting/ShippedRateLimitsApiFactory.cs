using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.RateLimiting;

/// <summary>
/// API host whose rate-limit policies are exactly the ones shipped in the API project's
/// <c>appsettings.json</c>: the shared test host lifts every limit, this one puts them back.
/// </summary>
public sealed class ShippedRateLimitsApiFactory : NotrelixApiFactory
{
    public static string ShippedAppSettingsPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var shipped = new ConfigurationBuilder()
                .AddJsonFile(ShippedAppSettingsPath, optional: false)
                .Build();

            config.AddInMemoryCollection(
                shipped.GetSection("RateLimiting:Policies").AsEnumerable(makePathsRelative: false)
                    .Where(pair => pair.Value is not null));
        });
    }
}
