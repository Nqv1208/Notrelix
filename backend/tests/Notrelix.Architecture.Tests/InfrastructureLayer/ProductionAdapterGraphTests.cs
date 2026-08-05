using System.Reflection;

namespace Notrelix.Architecture.Tests.InfrastructureLayer;

/// <summary>
/// INF-07 / DI-001..002: Production adapter graph verification.
/// Critical Application ports must resolve to real Infrastructure implementations.
/// Production types must not contain DevNull/NoOp/Fake/Stub/Mock/Placeholder.
/// </summary>
public class ProductionAdapterGraphTests
{
    private static readonly Assembly InfrastructureAssembly =
        typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).Assembly;

    private static readonly string[] ForbiddenProductionSuffixes =
    [
        "DevNull",
        "NoOp",
        "Fake",
        "Stub",
        "Mock",
        "Placeholder",
    ];

    /// <summary>
    /// Types that exist in Infrastructure but are conditionally registered only in
    /// Development/Testing with an environment guard that throws in Production.
    /// </summary>
    private static readonly HashSet<string> ConditionallyRegisteredDevTypes = new(StringComparer.Ordinal)
    {
        "Notrelix.Infrastructure.Billing.DevNullEntitlementChecker",
        "Notrelix.Infrastructure.Billing.DevNullSubscriptionChecker",
        "Notrelix.Infrastructure.Billing.DevNullFeatureGateChecker",
        "Notrelix.Infrastructure.Email.NoopEmailService",
        "Notrelix.Infrastructure.Integrations.Providers.NoopN8nClient",
        "Notrelix.Infrastructure.DevNullIntegrationEventBus",
        "Notrelix.Infrastructure.Messaging.DevNullRealtimePublisher",
    };

    [Fact]
    public void DI_001_No_Fake_Or_DevNull_Types_In_Production_Infrastructure()
    {
        var productionTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .ToList();

        var violations = productionTypes
            .Where(t => ForbiddenProductionSuffixes.Any(suffix =>
                t.Name.Contains(suffix, StringComparison.OrdinalIgnoreCase)))
            .Where(t => !ConditionallyRegisteredDevTypes.Contains(t.FullName!))
            .Select(t => t.FullName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        violations.Should().BeEmpty(
            "production Infrastructure assembly must not contain DevNull/NoOp/Fake/Stub/Mock/Placeholder types. " +
            "Tests must register fakes explicitly in their own composition root.");
    }

    [Fact]
    public void DI_001_Critical_Ports_Have_Real_Implementations()
    {
        var criticalPortNames = new[]
        {
            "IIdempotencyStore",
            "IRequestDataSession",
            "IRlsSessionContext",
            "IRedisCacheService",
            "IRealtimePublisher",
        };

        var implementationTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        var missing = new List<string>();

        foreach (var portName in criticalPortNames)
        {
            var hasImpl = implementationTypes.Any(t =>
                t.GetInterfaces().Any(i => i.Name == portName));

            if (!hasImpl)
            {
                missing.Add(portName);
            }
        }

        missing.Should().BeEmpty(
            "every critical Application port must have a production Infrastructure implementation");
    }

    [Fact]
    public void DI_002_IdempotencyStore_Implementation_Is_Not_Fake()
    {
        var storeImpl = InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => i.Name == "IIdempotencyStore"))
            .ToList();

        storeImpl.Should().NotBeEmpty("IIdempotencyStore must have a production implementation");

        foreach (var impl in storeImpl)
        {
            impl.Name.Should().NotContainAny(ForbiddenProductionSuffixes,
                $"idempotency store implementation '{impl.Name}' must be a real adapter");
        }
    }
}
