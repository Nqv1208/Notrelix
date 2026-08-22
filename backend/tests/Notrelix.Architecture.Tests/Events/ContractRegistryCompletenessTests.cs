using System.Reflection;
using MassTransit;
using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Architecture.Tests.Events;

public class ContractRegistryCompletenessTests
{
    private static readonly IReadOnlyList<ContractDefinition> Contracts =
        ContractRegistrySetup.GetContractDefinitions();

    private static readonly IContractRegistry Registry = new ContractRegistry(Contracts);

    private static readonly IReadOnlyList<Type> AllIntegrationEventTypes = typeof(IIntegrationEvent).Assembly
        .GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false }
                    && typeof(IIntegrationEvent).IsAssignableFrom(t))
        .ToList();

    [Fact]
    public void ContractRegistry_CoversAllIntegrationEvents()
    {
        var missing = AllIntegrationEventTypes
            .Where(t => !Registry.IsRegistered(t))
            .ToList();

        missing.Should().BeEmpty(
            $"all IIntegrationEvent implementors should have a ContractDefinition entry. " +
            $"Missing: {string.Join(", ", missing.Select(t => t.Name))}");
    }

    [Fact]
    public void ContractRegistry_AllEventsHaveEventNameAttribute()
    {
        var missing = AllIntegrationEventTypes
            .Where(t => t.GetCustomAttribute<EventNameAttribute>() is null)
            .ToList();

        missing.Should().BeEmpty(
            $"all IIntegrationEvent implementors should have [EventName]. " +
            $"Missing: {string.Join(", ", missing.Select(t => t.Name))}");
    }

    /// <summary>
    /// IA-TST-EVT-VER-ARCH-001 / IAREQ088 / IAREQ131 — public integration-event
    /// uniqueness is the compound identity (Name, Version). The same logical
    /// name MAY coexist as multiple versions during a controlled migration
    /// window; only an exact (Name, Version) collision is a defect.
    /// </summary>
    [Fact]
    public void ContractRegistry_AllEventsHaveUniqueCompoundContractIdentity()
    {
        var duplicates = Contracts
            .GroupBy(c => (c.Name.ToLowerInvariant(), c.Version))
            .Where(g => g.Count() > 1)
            .ToList();

        duplicates.Should().BeEmpty(
            "public integration-event contract identity is (Name, Version); the same logical name may " +
            $"coexist across versions but an exact version collision is a defect. Duplicates: " +
            $"{string.Join(", ", duplicates.Select(g => $"'{g.Key.Item1}' v{g.Key.Item2} ({g.Count()}x)"))}");
    }

    [Fact]
    public void ContractRegistry_AllVersionsArePositive()
    {
        var invalid = Contracts.Where(c => c.Version <= 0).ToList();

        invalid.Should().BeEmpty(
            $"public event versions must be positive integers. Invalid: " +
            $"{string.Join(", ", invalid.Select(c => $"{c.Name} v{c.Version}"))}");
    }
}

public class ConsumerRegistryCompletenessTests
{
    private static readonly IReadOnlyList<ConsumerDefinition> ConsumerDefs =
        ConsumerRegistrySetup.GetConsumerDefinitions();

    private static readonly ConsumerRegistry Registry = new(ConsumerDefs);

    private static readonly IReadOnlyList<Type> AllConsumerTypes = typeof(ConsumerRegistry).Assembly
        .GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false })
        .Where(t => t.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)))
        .ToList();

    [Fact]
    public void ConsumerRegistry_CoversAllMassTransitConsumers()
    {
        var eventNames = ConsumerDefs
            .Select(c => c.EventName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = new List<string>();

        foreach (var consumerType in AllConsumerTypes)
        {
            var consumedEventTypes = consumerType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            foreach (var eventType in consumedEventTypes)
            {
                var eventNameAttr = eventType.GetCustomAttribute<EventNameAttribute>();
                var eventName = eventNameAttr?.Name ?? eventType.FullName ?? eventType.Name;

                if (!eventNames.Contains(eventName))
                {
                    missing.Add($"{consumerType.Name} consumes '{eventName}' which is not in ConsumerRegistry");
                }
            }
        }

        missing.Should().BeEmpty(
            $"all MassTransit IConsumer<T> types should have their event name in ConsumerRegistry. " +
            $"Missing:\n  {string.Join("\n  ", missing)}");
    }
}
