using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure.Tests.Runtime;

/// <summary>
/// The worker's hosted services are registered only when the worker role is selected. Every other role
/// starts no background loop; the readiness-only role guard is the one hosted service every role keeps.
/// </summary>
public class RuntimeRolesRegistrationTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    private static int WorkerHostedServiceCount(RuntimeRoles roles)
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs(EmptyConfig(), roles);

        return services.Count(d => d.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void WithoutWorkerRole_NoWorkerHostedServiceIsRegistered()
    {
        WorkerHostedServiceCount(RuntimeRoles.FromTokens(["api"])).Should().Be(0);
        WorkerHostedServiceCount(RuntimeRoles.FromTokens(["realtime"])).Should().Be(0);
        WorkerHostedServiceCount(RuntimeRoles.FromTokens(["api", "realtime"])).Should().Be(0);
    }

    [Fact]
    public void WithWorkerRole_EveryBackgroundJobIsRegistered()
    {
        WorkerHostedServiceCount(RuntimeRoles.FromTokens(["worker"])).Should().Be(14);
        WorkerHostedServiceCount(RuntimeRoles.All).Should().Be(14);
    }

    [Fact]
    public void DefaultRoles_AreAllRoles()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs(EmptyConfig());

        services.Count(d => d.ServiceType == typeof(IHostedService)).Should().Be(14);
    }
}
