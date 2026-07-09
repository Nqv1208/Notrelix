using System.Reflection;

namespace Notrelix.Infrastructure.Messaging;

public sealed class IntegrationEventCatalog : IIntegrationEventCatalog
{
    private readonly IReadOnlyDictionary<string, Type> _nameToType;

    public IntegrationEventCatalog()
    {
        var entries = new Dictionary<string, Type>(StringComparer.Ordinal);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t))
            .Distinct();

        foreach (var type in types)
        {
            var name = ResolveName(type);
            if (!entries.TryAdd(name, type))
            {
                throw new InvalidOperationException(
                    $"Duplicate integration event name '{name}' detected. " +
                    $"Types: '{entries[name].FullName}' and '{type.FullName}'. " +
                    "Event names must be unique.");
            }
        }

        _nameToType = entries;
    }

    public Type Resolve(string messageName)
    {
        if (_nameToType.TryGetValue(messageName, out var type))
            return type;

        throw new UnknownIntegrationEventTypeException(messageName);
    }

    public bool TryResolve(string messageName, out Type type)
    {
        return _nameToType.TryGetValue(messageName, out type!);
    }

    private static string ResolveName(Type type)
    {
        var attr = type.GetCustomAttribute<EventNameAttribute>();
        if (attr is not null)
            return attr.Name;

        return type.FullName ?? type.Name;
    }
}
