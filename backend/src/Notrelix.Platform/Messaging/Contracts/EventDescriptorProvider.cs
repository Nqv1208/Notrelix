using System.Collections.Concurrent;
using System.Reflection;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;

namespace Notrelix.Platform.Messaging.Contracts;

public sealed class UnknownEventDescriptorException : InvalidOperationException
{
    public UnknownEventDescriptorException(string message)
        : base(message)
    {
    }
}

public sealed class EventDescriptorProvider : IEventDescriptorProvider
{
    private readonly ConcurrentDictionary<string, EventDescriptor> _byName;
    private readonly ConcurrentDictionary<Type, EventDescriptor> _byType;

    public EventDescriptorProvider()
    {
        _byName = new ConcurrentDictionary<string, EventDescriptor>(StringComparer.Ordinal);
        _byType = new ConcurrentDictionary<Type, EventDescriptor>();

        LoadDescriptors();
    }

    public EventDescriptor Get(Type eventType)
    {
        if (_byType.TryGetValue(eventType, out var descriptor))
            return descriptor;

        throw new UnknownEventDescriptorException(
            $"No EventDescriptor registered for type '{eventType.FullName}'.");
    }

    public EventDescriptor Get(string eventName, int version)
    {
        var key = BuildKey(eventName, version);
        if (_byName.TryGetValue(key, out var descriptor))
            return descriptor;

        throw new UnknownEventDescriptorException(
            $"No EventDescriptor registered for event '{eventName}' version {version}.");
    }

    private void LoadDescriptors()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var eventTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IIntegrationEvent).IsAssignableFrom(t))
            .Distinct()
            .ToList();

        foreach (var type in eventTypes)
        {
            var attr = type.GetCustomAttribute<EventNameAttribute>();
            if (attr is null)
                continue;

            var descriptor = new EventDescriptor
            {
                Name = attr.Name,
                Version = attr.Version,
                EventType = type,
                Classification = EventClassification.Business,
            };

            _byType.TryAdd(type, descriptor);
            _byName.TryAdd(BuildKey(attr.Name, attr.Version), descriptor);
        }
    }

    private static string BuildKey(string eventName, int version)
        => $"{eventName}:v{version}";
}
