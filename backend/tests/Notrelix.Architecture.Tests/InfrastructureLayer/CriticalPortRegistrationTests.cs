using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Data.Rls;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Time;

namespace Notrelix.Architecture.Tests.InfrastructureLayer;

/// <summary>
/// Verifies that critical Application ports have Infrastructure adapters registered.
/// These ports must not silently disappear from DI — a missing adapter is a startup failure.
/// </summary>
public sealed class CriticalPortRegistrationTests
{
    [Fact]
    public void Application_ports_must_have_infrastructure_adapters()
    {
        var infrastructureAssembly = typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).Assembly;

        var criticalPorts = new (Type Port, string Description)[]
        {
            (typeof(IRequestDataSession), "transaction/RLS/SaveChanges mechanics"),
            (typeof(IRlsSessionContext), "row-level security session context"),
            (typeof(IDateTimeProvider), "deterministic time provider"),
        };

        var missing = new List<string>();

        foreach (var (port, description) in criticalPorts)
        {
            var implementations = infrastructureAssembly
                .GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .Where(t => port.IsAssignableFrom(t))
                .ToList();

            if (implementations.Count == 0)
                missing.Add($"{port.Name} ({description}): no Infrastructure implementation found");
        }

        missing.Should().BeEmpty(
            "every critical Application port must have at least one Infrastructure adapter. Missing:\n" +
            string.Join("\n", missing));
    }

    [Fact]
    public void IdempotencyStore_must_have_infrastructure_implementation()
    {
        var infrastructureAssembly = typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).Assembly;

        var implementations = infrastructureAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IIdempotencyStore).IsAssignableFrom(t))
            .ToList();

        implementations.Should().NotBeEmpty(
            "IIdempotencyStore must have at least one Infrastructure implementation");
    }
}
