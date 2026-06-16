using System.Collections.Concurrent;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Data.Outbox;

public sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _cache;

    public EventTypeRegistry()
    {
        _cache = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal);

        var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) || t.Name.EndsWith("Event"))
            .Distinct();

        foreach (var type in eventTypes)
        {
            if (type.FullName is not null)
                _cache.TryAdd(type.FullName, type);
            _cache.TryAdd(type.Name, type);
        }
    }

    public Type? GetEventType(string eventTypeName)
    {
        return _cache.TryGetValue(eventTypeName, out var type) ? type : null;
    }
}
