using System.Reflection;

namespace Notrelix.Infrastructure.Messaging;

/// <summary>
/// Version-aware public integration-event catalog (IAREQ131 / P13-EVT-003A).
/// Uniqueness is (Name, Version): the same logical name MAY coexist as multiple
/// versions for a controlled migration window; registering the same
/// (Name, Version) twice is a composition error. Unknown names and unsupported
/// versions fail deterministically — no latest/oldest/v1 fallback exists.
/// </summary>
public sealed class IntegrationEventCatalog : IIntegrationEventCatalog
{
    private readonly IReadOnlyDictionary<EventContractKey, Type> _contractsByKey;

    public IntegrationEventCatalog()
        : this(DiscoverIntegrationEventTypes())
    {
    }

    /// <summary>
    /// Explicit type-seeded composition used by focused tests and tooling so
    /// v1/v2 coexistence can be proven without loading fixture assemblies into
    /// the production discovery surface.
    /// </summary>
    public IntegrationEventCatalog(IEnumerable<Type> integrationEventTypes)
    {
        var entries = new Dictionary<EventContractKey, Type>();

        foreach (var type in integrationEventTypes)
        {
            var key = ResolveKey(type);
            if (!entries.TryAdd(key, type))
            {
                throw new InvalidOperationException(
                    $"Duplicate integration event contract '{key}' detected. " +
                    $"Types: '{entries[key].FullName}' and '{type.FullName}'. " +
                    "The compound contract identity (Name, Version) must be unique.");
            }
        }

        _contractsByKey = entries;
    }

    private static IEnumerable<Type> DiscoverIntegrationEventTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t))
            .Distinct();
    }

    /// <summary>
    /// Factory for DI registration. Required because the container would
    /// otherwise prefer the <c>IEnumerable&lt;Type&gt;</c> constructor and bind it
    /// to an empty sequence, yielding a catalog that resolves nothing.
    /// </summary>
    public static IntegrationEventCatalog FromAppDomain() =>
        new(DiscoverIntegrationEventTypes());

    public Type Resolve(EventContractKey key)
    {
        if (_contractsByKey.TryGetValue(key, out var type))
            return type;

        throw new UnknownIntegrationEventTypeException(key.Name);
    }

    public bool TryResolve(EventContractKey key, out Type type)
    {
        return _contractsByKey.TryGetValue(key, out type!);
    }

    private static EventContractKey ResolveKey(Type type)
    {
        var attr = type.GetCustomAttribute<EventNameAttribute>();
        if (attr is not null)
            return new EventContractKey(attr.Name, attr.Version);

        // Contracts without explicit event metadata are not governed public
        // contracts; they are indexed under their CLR name with version 1 so the
        // uniqueness gate still sees them. Public producers must carry EventName.
        return new EventContractKey(type.FullName ?? type.Name, 1);
    }
}
