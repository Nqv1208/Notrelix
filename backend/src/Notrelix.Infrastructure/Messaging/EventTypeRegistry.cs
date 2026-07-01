using System.Collections.Concurrent;
using System.Reflection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;

namespace Notrelix.Infrastructure.Messaging;

public sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _nameToType;
    private readonly ConcurrentDictionary<Type, string> _typeToName;

    public EventTypeRegistry()
    {
        _nameToType = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal);
        _typeToName = new ConcurrentDictionary<Type, string>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) || typeof(IIntegrationEvent).IsAssignableFrom(t))
            .Distinct();

        foreach (var type in types)
        {
            var name = ResolveName(type);
            _nameToType.TryAdd(name, type);
            _typeToName.TryAdd(type, name);
        }
    }

    public Type? GetEventType(string messageName)
    {
        return _nameToType.TryGetValue(messageName, out var type) ? type : null;
    }

    public string GetMessageName(Type type)
    {
        if (_typeToName.TryGetValue(type, out var name))
            return name;
        return ResolveName(type);
    }

    public string GetMessageName<T>()
    {
        return GetMessageName(typeof(T));
    }

    private static string ResolveName(Type type)
    {
        var attr = type.GetCustomAttribute<EventNameAttribute>();
        if (attr is not null)
            return attr.Name;

        return type.FullName ?? type.Name;
    }
}
